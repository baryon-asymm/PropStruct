#!/usr/bin/env python3
"""Proof that scan.py is non-degenerate: red on each thing it guards and right on the
same tree without it (AGENTS.md 13; root BOOT.md, taboo "proven twice").

Everything here is synthetic: a made-up source, a made-up executable and a made-up
forbidden list. The self-test needs no original and holds no byte, line or statement of
it; the scan against the real original is the `Legacy` category's, in `Fixtures.Tests`.

    python -m unittest tools/legacy/test_scan.py
"""

from __future__ import annotations

import contextlib
import hashlib
import io
import os
import struct
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Dict, List

sys.path.insert(0, str(Path(__file__).resolve().parent))

import scan  # noqa: E402

SYNTHETIC_STATEMENTS = [
    "      alpha{n} = sqrt(beta{n}*gamma{n} + delta{n}*epsilon{n}) + zeta{n}".format(n=n)
    for n in range(1, 13)
]
SYNTHETIC_SOURCE = "\n".join(
    ["c  a comment line that quotes nothing of the program at all, however long it runs",
     "\tUSE SYNTHETICMODULE",
     "      write(*,*) alphaprinted, betaprinted, gammaprinted, deltaprinted, more",
     "      data tablevalues /1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0/",
     "  17  format(1x,a,f6.1,a,f6.1,a,f5.3,a,f5.3,a,f5.3,a,f5.3,a,f5.3)",
     "      message = 'a text between quotes that no fragment may carry'"]
    + SYNTHETIC_STATEMENTS
    + ["      total = first_term +", "     &        second_term_of_the_total"]
) + "\n"
CONTINUED_STATEMENT = "total=first_term+second_term_of_the_total"


def synthetic_image() -> bytes:
    """A minimal PE image with one code section and one data section."""
    code = bytes(hashlib.sha256(b"code%d" % index).digest()[0] for index in range(400)) + b"\x90" * 32
    data = bytes(hashlib.sha256(b"data%d" % index).digest()[0] for index in range(300))
    pe_offset = 0x40
    header = bytearray(0x40)
    struct.pack_into("<I", header, 0x3C, pe_offset)
    coff = b"PE\0\0" + struct.pack("<HHIIIHH", 0x14C, 2, 0, 0, 0, 0, 0)
    first_raw = 0x200

    def section(name: bytes, size: int, pointer: int, characteristics: int) -> bytes:
        return name.ljust(8, b"\0") + struct.pack("<IIIIIIHHI", size, 0x1000, size, pointer, 0, 0, 0, 0, characteristics)

    sections = section(b".text", len(code), first_raw, 0x60000020) + section(b".data", len(data), first_raw + 0x200, 0xC0000040)
    image = bytearray(header) + coff + sections
    image = image.ljust(first_raw, b"\0") + code
    image = image.ljust(first_raw + 0x200, b"\0") + data
    return bytes(image)


IMAGE = synthetic_image()
CODE = scan.code_section(IMAGE)


def reference() -> scan.Reference:
    statements = scan.source_statements(SYNTHETIC_SOURCE)
    return scan.Reference(scan.code_windows([IMAGE]), scan.statement_fragments(statements), len(statements))


def run_scan(files: Dict[str, bytes], forbidden: frozenset = frozenset(), full: bool = True) -> scan.ScanResult:
    with tempfile.TemporaryDirectory(prefix="propstruct_scan_test_") as temporary:
        root = Path(temporary)
        for name, data in files.items():
            target = root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
        return scan.scan_tree(root, forbidden, reference() if full else None)


def kinds(result: scan.ScanResult) -> List[str]:
    return sorted(finding.kind for finding in result.findings)


class SourceParsingTest(unittest.TestCase):
    def test_statements_exclude_comments_and_join_continuations(self) -> None:
        statements = scan.source_statements(SYNTHETIC_SOURCE)
        texts = [scan.normalise(text) for _, text in statements]
        self.assertNotIn("acommentlinethatquotesnothing", "".join(texts))
        self.assertIn(CONTINUED_STATEMENT, texts)
        self.assertEqual(len(statements), 6 + len(SYNTHETIC_STATEMENTS) + 1 - 1)

    def test_tab_form_lines_and_labels_are_read(self) -> None:
        statements = scan.source_statements("\tUSE ONE\n 17\tFORMAT(1x,a)\n\tcall second(argument)\n\t1 + continued\n")
        self.assertEqual([text.strip() for _, text in statements], ["USE ONE", "FORMAT(1x,a)", "call second(argument) + continued"])

    def test_io_statements_and_quoted_text_give_no_fragment(self) -> None:
        fragments = scan.statement_fragments(scan.source_statements(SYNTHETIC_SOURCE))
        joined = " ".join(fragments)
        for absent in ("alphaprinted", "tablevalues", "1x,a,f6.1", "textbetweenquotes"):
            self.assertNotIn(absent, joined)
        self.assertTrue(any("alpha1=sqrt" in fragment for fragment in fragments))

    def test_fragments_have_the_fixed_length(self) -> None:
        fragments = scan.statement_fragments(scan.source_statements(SYNTHETIC_SOURCE))
        self.assertTrue(fragments)
        self.assertEqual({len(fragment) for fragment in fragments}, {scan.FRAGMENT_CHARACTERS})


class CodeWindowTest(unittest.TestCase):
    def test_only_code_sections_give_windows(self) -> None:
        self.assertEqual(len(CODE), 432)
        windows = scan.code_windows([IMAGE])
        self.assertIn(CODE[:scan.WINDOW_BYTES], windows)
        self.assertIn(CODE[400 - scan.WINDOW_BYTES:400], windows)
        self.assertNotIn(IMAGE[0x400:0x410], windows)

    def test_padding_windows_are_not_kept(self) -> None:
        padded = bytearray(IMAGE)
        padded[0x200:0x200 + 64] = b"\xcc" * 64
        windows = scan.code_windows([bytes(padded)])
        self.assertNotIn(b"\xcc" * 16, windows)


class ScanTest(unittest.TestCase):
    def test_a_clean_tree_is_right(self) -> None:
        result = run_scan({"a.txt": b"nothing of the program\n", "b/c.cs": b"class C {}\n"})
        self.assertEqual(result.findings, [])
        self.assertEqual(result.files, 2)

    def test_forbidden_hash_is_a_finding_and_names_the_file(self) -> None:
        planted = b"planted bytes of a forbidden file"
        digest = hashlib.sha256(planted).hexdigest()
        result = run_scan({"docs/copy.bin": planted, "other.txt": b"x"}, frozenset((digest,)), full=False)
        self.assertEqual([f.render() for f in result.findings], ["hash docs/copy.bin"])

    def test_empty_file_is_not_a_finding_even_when_its_hash_is_listed(self) -> None:
        empty = hashlib.sha256(b"").hexdigest()
        result = run_scan({"__init__.py": b""}, frozenset((empty,)), full=False)
        self.assertEqual(result.findings, [])

    def test_build_product_names_are_findings(self) -> None:
        names = ["x.exe", "lib/y.DLL", "z.zip", "a.obj", "b.pdb", "c.for", "d.dsp", "e.dsw", "f.opt", "g.plg", "h.for.txt"]
        result = run_scan({name: b"x" for name in names + ["fine.formula", "fortune.txt"]}, full=False)
        self.assertEqual(sorted(f.file for f in result.findings), sorted(names))
        self.assertTrue(all(f.kind == "name" for f in result.findings))

    def test_hashes_only_reads_no_statement_and_no_byte(self) -> None:
        planted = SYNTHETIC_SOURCE.encode("utf-8") + CODE
        result = run_scan({"planted.txt": planted}, full=False)
        self.assertEqual(result.findings, [])

    def test_raw_code_bytes_are_a_finding_with_their_offset(self) -> None:
        result = run_scan({"blob.dat": b"header" + CODE[100:140] + b"tail"})
        self.assertEqual([(f.kind, f.file, f.offset) for f in result.findings], [("bytes", "blob.dat", 6)])

    def test_a_run_shorter_than_the_window_is_not_a_finding(self) -> None:
        result = run_scan({"short.dat": b"header" + CODE[100:115] + b"tail"})
        self.assertEqual(result.findings, [])
        result = run_scan({"exact.dat": b"header" + CODE[100:116] + b"tail"})
        self.assertEqual(kinds(result), ["bytes"])

    def test_code_bytes_in_the_columns_of_a_listing_are_a_finding(self) -> None:
        rows = []
        for start in range(0, 60, 6):
            chunk = CODE[start:start + 6]
            rows.append(f"  {0x401000 + start:08X}: " + " ".join(f"{b:02X}" for b in chunk) + "   nop")
        result = run_scan({"listing.txt": "\n".join(rows).encode("ascii")})
        self.assertEqual(kinds(result), ["bytes"])

    def test_code_bytes_in_a_run_of_hexadecimal_digits_are_a_finding(self) -> None:
        result = run_scan({"dump.json": ('{"blob": "' + CODE[200:240].hex() + '"}').encode("ascii")})
        self.assertEqual(kinds(result), ["bytes"])

    def test_a_window_of_padding_is_not_a_finding(self) -> None:
        result = run_scan({"padding.bin": b"\x90" * 64})
        self.assertEqual(result.findings, [])

    def test_nine_statements_in_one_file_are_nine_findings_with_lines(self) -> None:
        block = "\n".join(["// the block", *[s.strip() for s in SYNTHETIC_STATEMENTS[:9]]]) + "\n"
        result = run_scan({"Sample.cs": block.encode("utf-8")})
        self.assertEqual(kinds(result), ["statement"] * 9)
        self.assertEqual([f.line for f in result.findings], list(range(2, 11)))
        self.assertEqual(len({f.source_line for f in result.findings}), 9)
        self.assertEqual(result.most_statements, (9, "Sample.cs"))

    def test_eight_statements_in_one_file_are_right_and_counted(self) -> None:
        block = "\n".join(s.strip() for s in SYNTHETIC_STATEMENTS[:8]) + "\n"
        result = run_scan({"Sample.cs": block.encode("utf-8")})
        self.assertEqual(result.findings, [])
        self.assertEqual(result.most_statements, (8, "Sample.cs"))

    def test_statements_split_over_files_are_right(self) -> None:
        result = run_scan({
            "one.cs": "\n".join(s.strip() for s in SYNTHETIC_STATEMENTS[:6]).encode("utf-8"),
            "two.cs": "\n".join(s.strip() for s in SYNTHETIC_STATEMENTS[6:12]).encode("utf-8"),
        })
        self.assertEqual(result.findings, [])

    def test_whitespace_and_case_do_not_hide_a_statement(self) -> None:
        shouted = "\n".join(s.strip().upper().replace(" ", "  ") for s in SYNTHETIC_STATEMENTS[:9]) + "\n"
        result = run_scan({"Shout.cs": shouted.encode("utf-8")})
        self.assertEqual(kinds(result), ["statement"] * 9)

    def test_the_original_s_data_and_outputs_are_not_scanned_for_statements(self) -> None:
        block = "\n".join(s.strip() for s in SYNTHETIC_STATEMENTS[:12]).encode("utf-8")
        result = run_scan({"tests/Fixtures/Legacy/formulations/x.dat": block, "tests/Fixtures/references/a/results.m.txt": block})
        self.assertEqual(result.findings, [])

    def test_the_skipped_directories_are_not_walked(self) -> None:
        result = run_scan({".git/config": b"x", "src/bin/Debug/a.dll": b"x", "src/obj/b.exe": b"x",
                           "tools/__pycache__/a.exe": b"x", ".claude/worktrees/other/a.exe": b"x", "src/keep.txt": b"x"}, full=False)
        self.assertEqual(result.findings, [])
        self.assertEqual(result.files, 1)

    def test_only_the_worktrees_of_the_agent_directory_are_skipped(self) -> None:
        result = run_scan({".claude/agents/a.exe": b"x", "worktrees/b.exe": b"x", "src/.claude/c.txt": b"x"}, full=False)
        self.assertEqual(sorted(f.file for f in result.findings), [".claude/agents/a.exe", "worktrees/b.exe"])

    def test_a_binary_file_is_not_read_as_text(self) -> None:
        result = run_scan({"image.png": bytes(range(128, 256)) * 4})
        self.assertEqual(result.findings, [])


SHORT_STATEMENTS = [f"      rho{n}=a{n}+b{n}*c{n}" for n in range(1, 13)]  # 13 to 17 characters, normalised
QUOTATIONS = {
    "backtick span in Markdown": ("Doc.md", lambda statement: f"The line `{statement.strip()}` does it.\n"),
    "double backtick span": ("Doc.md", lambda statement: f"The line ``{statement.strip()}`` does it.\n"),
    "fenced block in Markdown": ("Doc.md", lambda statement: f"```text\n{statement}\n```\n"),
    "tilde fence in Markdown": ("Doc.md", lambda statement: f"~~~text\n{statement}\n~~~\n"),
    "c element of an XML doc": ("Sample.cs", lambda statement: f"/// <c>{statement.strip()}</c>\n"),
    "comment line of code": ("Sample.cs", lambda statement: f"// {statement.strip()}\n"),
    "hash comment line": ("tool.py", lambda statement: f"    # {statement.strip()}\n"),
}


def short_scan(name: str, text: str, source: str = "\n".join(SHORT_STATEMENTS) + "\n") -> scan.ScanResult:
    with tempfile.TemporaryDirectory(prefix="propstruct_scan_test_") as temporary:
        root = Path(temporary)
        (root / name).write_text(text, encoding="utf-8")
        return scan.scan_tree(root, frozenset(), scan.build_reference([], source))


def quoted(statements: List[str]) -> str:
    return "".join(f"`{statement}`\n" for statement in statements)


def program(statements: List[str]) -> str:
    return "\n".join("      " + statement for statement in statements) + "\n"


class ShortStatementTest(unittest.TestCase):
    """A statement shorter than a fragment is counted whole, in quoted text only."""

    def test_the_statements_are_shorter_than_a_fragment_and_not_shorter_than_the_floor(self) -> None:
        lengths = [len(scan.normalise(text)) for text in SHORT_STATEMENTS]
        self.assertTrue(all(scan.SHORT_STATEMENT_FLOOR <= length < scan.FRAGMENT_CHARACTERS for length in lengths))
        self.assertEqual(scan.statement_fragments(scan.source_statements("\n".join(SHORT_STATEMENTS))), {})

    def test_the_reference_line_counts_the_short_statements(self) -> None:
        searched = scan.build_reference([], "\n".join(SHORT_STATEMENTS) + "\n")
        self.assertEqual(scan.reference_line(searched),
                         f"reference: {len(SHORT_STATEMENTS)} statements, 0 fragments, "
                         f"{len(SHORT_STATEMENTS)} short statements, 0 code windows")

    def test_nine_quoted_in_every_form_are_nine_findings_with_lines(self) -> None:
        for form, (name, quote) in QUOTATIONS.items():
            with self.subTest(form):
                result = short_scan(name, "".join(quote(statement) for statement in SHORT_STATEMENTS[:9]))
                self.assertEqual(kinds(result), ["statement"] * 9)
                self.assertEqual(len({finding.source_line for finding in result.findings}), 9)
                self.assertEqual(result.most_statements, (9, name))
                lines = [finding.line for finding in result.findings]
                self.assertEqual(lines, sorted(lines))

    def test_eight_quoted_in_every_form_are_right_and_counted(self) -> None:
        for form, (name, quote) in QUOTATIONS.items():
            with self.subTest(form):
                result = short_scan(name, "".join(quote(statement) for statement in SHORT_STATEMENTS[:8]))
                self.assertEqual(result.findings, [])
                self.assertEqual(result.most_statements, (8, name))

    def test_the_prose_after_a_closed_fence_is_not_a_span(self) -> None:
        text = "```text\nnothing\n```\n" + "".join(f"{s.strip()}\n" for s in SHORT_STATEMENTS[:9])
        self.assertEqual(short_scan("Doc.md", text).findings, [])

    def test_a_fence_is_a_markdown_notion_only(self) -> None:
        text = "```text\n" + "".join(f"{s.strip()}\n" for s in SHORT_STATEMENTS[:9]) + "```\n"
        self.assertEqual(short_scan("tool.cfg", text).findings, [])
        self.assertEqual(kinds(short_scan("Doc.md", text)), ["statement"] * 9)

    def test_a_markdown_heading_is_not_a_comment_line_and_a_hash_line_of_code_is(self) -> None:
        text = "".join(f"# {s.strip()}\n" for s in SHORT_STATEMENTS[:9])
        self.assertEqual(short_scan("Doc.md", text).findings, [])
        self.assertEqual(kinds(short_scan("tool.py", text)), ["statement"] * 9)

    def test_the_statement_in_unquoted_prose_or_with_more_around_it_is_not_counted(self) -> None:
        prose = "".join(f"The original does {s.strip()} here, and then more.\n" for s in SHORT_STATEMENTS)
        self.assertEqual(short_scan("Doc.md", prose).findings, [])
        wider = "".join(f"`{s.strip()} + extra` and `{s.strip()};` here\n" for s in SHORT_STATEMENTS)
        self.assertEqual(short_scan("Doc.md", wider).findings, [])

    def test_case_and_spacing_do_not_hide_a_quoted_statement(self) -> None:
        text = "".join(f"`{s.strip().upper().replace('=', ' = ')}`\n" for s in SHORT_STATEMENTS[:9])
        self.assertEqual(kinds(short_scan("Doc.md", text)), ["statement"] * 9)

    def test_the_floor_counts_a_statement_at_the_floor_and_not_below_it(self) -> None:
        floor = scan.SHORT_STATEMENT_FLOOR
        at_the_floor = [f"q{n}=" + "w" * (floor - 3) for n in range(1, 10)]
        below = [f"q{n}=" + "w" * (floor - 4) for n in range(1, 10)]
        self.assertEqual({len(statement) for statement in at_the_floor}, {floor})
        for statements, expected in ((at_the_floor, ["statement"] * 9), (below, [])):
            self.assertEqual(kinds(short_scan("Doc.md", quoted(statements), program(statements))), expected)

    def test_trivial_statements_are_not_counted(self) -> None:
        trivial = [f"a{n}=0" for n in range(1, 13)] + ["continue", "endif", "return"]
        self.assertEqual(short_scan("Doc.md", quoted(trivial), program(trivial)).findings, [])

    def test_the_longest_short_statement_is_counted_here_and_the_next_one_by_its_fragments(self) -> None:
        length = scan.FRAGMENT_CHARACTERS
        short = [f"v{n}=" + "x" * (length - 3 - len(str(n))) for n in range(10, 19)]
        long = [f"v{n}=" + "x" * (length - 2 - len(str(n))) for n in range(10, 19)]
        self.assertEqual({len(statement) for statement in short}, {length - 1})
        self.assertEqual({len(statement) for statement in long}, {length})
        for statements in (short, long):
            self.assertEqual(kinds(short_scan("Doc.md", quoted(statements), program(statements))), ["statement"] * 9)

    def test_output_format_and_quoted_text_are_no_short_statement(self) -> None:
        source = "      write(2,1000) alpha\n      data xx /1/\n      msg = 'a b c d e'\n      print*, rr\n"
        self.assertEqual(scan.short_statement_texts(scan.source_statements(source)), {})

    def test_a_statement_is_named_at_its_first_quotation_and_its_first_source_line(self) -> None:
        statements = [s.strip() for s in SHORT_STATEMENTS[:9]]
        result = short_scan("Doc.md", quoted(statements) + quoted(statements[::-1]), program(statements + statements))
        self.assertEqual([finding.line for finding in result.findings], list(range(1, 10)))
        self.assertEqual([finding.source_line for finding in result.findings], list(range(1, 10)))

    def test_the_findings_are_in_the_order_of_the_text_and_not_of_the_source(self) -> None:
        statements = [s.strip() for s in SHORT_STATEMENTS[:9]]
        result = short_scan("Doc.md", quoted(statements[::-1]), program(statements))
        self.assertEqual([finding.line for finding in result.findings], list(range(1, 10)))
        self.assertEqual([finding.source_line for finding in result.findings], list(range(9, 0, -1)))

    def test_a_repeated_statement_is_one_statement(self) -> None:
        source = program(["rho1=a1+b1*c1"] * 5)
        result = short_scan("Doc.md", quoted(["rho1=a1+b1*c1"] * 20), source)
        self.assertEqual((result.findings, result.most_statements), ([], (1, "Doc.md")))

    def test_the_original_s_outputs_are_not_scanned_for_short_statements_either(self) -> None:
        self.assertEqual(short_scan("x.m.txt", quoted([s.strip() for s in SHORT_STATEMENTS])).findings, [])

    def test_no_short_finding_prints_the_statement(self) -> None:
        result = short_scan("Doc.md", quoted([s.strip() for s in SHORT_STATEMENTS]))
        self.assertTrue(result.findings)
        self.assertNotIn("rho", "\n".join(finding.render() for finding in result.findings))


class OutputTest(unittest.TestCase):
    def test_no_finding_prints_the_matched_text_or_bytes(self) -> None:
        block = "\n".join(s.strip() for s in SYNTHETIC_STATEMENTS[:10]) + "\n"
        planted = {"Sample.cs": block.encode("utf-8"), "blob.dat": CODE[100:140]}
        result = run_scan(planted)
        printed = "\n".join(finding.render() for finding in result.findings) + "\n" + "\n".join(scan.summary(result, False))
        self.assertTrue(result.findings)
        for secret in ("alpha1", "sqrt", "beta1", CODE[100:116].hex(), CODE[100:116].decode("latin1")):
            self.assertNotIn(secret, printed)

    def test_a_statement_finding_names_the_file_the_line_and_the_source_line(self) -> None:
        block = "\n".join(["// the block", *[s.strip() for s in SYNTHETIC_STATEMENTS[:9]]]) + "\n"
        result = run_scan({"Sample.cs": block.encode("utf-8")})
        source_lines = SYNTHETIC_SOURCE.split("\n")
        expected = [f"statement Sample.cs:{number} source line {source_lines.index(statement) + 1}"
                    for number, statement in enumerate(SYNTHETIC_STATEMENTS[:9], start=2)]
        self.assertEqual([f.render() for f in result.findings], expected)

    def test_the_reference_line_counts_what_was_searched_for(self) -> None:
        searched = reference()
        statements = scan.source_statements(SYNTHETIC_SOURCE)
        self.assertEqual(
            scan.reference_line(searched),
            f"reference: {len(statements)} statements, {len(searched.fragments)} fragments, "
            f"{len(searched.short_statements)} short statements, {len(searched.windows)} code windows")
        self.assertEqual(searched.statement_count, len(statements))

    def test_the_summary_line_counts_by_kind(self) -> None:
        block = "\n".join(s.strip() for s in SYNTHETIC_STATEMENTS[:9]) + "\n"
        result = run_scan({"Sample.cs": block.encode("utf-8"), "a.exe": b"x"})
        self.assertEqual(
            scan.summary(result, False),
            ["scan: 2 files; forbidden hashes 0, names 1, code windows 0, statements 9",
             "most statements in one file: 9 (Sample.cs)"],
        )
        self.assertEqual(scan.summary(result, True)[0], "scan: 2 files; forbidden hashes 0, names 1, code windows -, statements -")


class CommandTest(unittest.TestCase):
    def run_main(self, arguments: List[str], **environment: str) -> int:
        saved = os.environ.pop(scan.legacy.ENVIRONMENT_VARIABLE, None)
        os.environ.update(environment)
        try:
            with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
                return scan.main(arguments)
        finally:
            os.environ.pop(scan.legacy.ENVIRONMENT_VARIABLE, None)
            if saved is not None:
                os.environ[scan.legacy.ENVIRONMENT_VARIABLE] = saved

    def test_hashes_runs_without_the_variable_and_is_clean_on_a_clean_tree(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_scan_test_") as temporary:
            (Path(temporary) / "a.txt").write_text("nothing\n", encoding="utf-8")
            self.assertEqual(self.run_main([temporary, "--hashes"]), scan.EXIT_CLEAN)

    def test_hashes_is_red_on_a_build_product_name(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_scan_test_") as temporary:
            (Path(temporary) / "a.exe").write_bytes(b"x")
            self.assertEqual(self.run_main([temporary, "--hashes"]), scan.EXIT_FINDINGS)

    def test_the_full_scan_needs_the_variable(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_scan_test_") as temporary:
            self.assertEqual(self.run_main([temporary]), scan.EXIT_USAGE)

    def test_the_full_scan_refuses_a_directory_that_is_not_the_original(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_scan_test_") as temporary, \
                tempfile.TemporaryDirectory(prefix="propstruct_scan_fake_") as fake:
            self.assertEqual(self.run_main([temporary], **{scan.legacy.ENVIRONMENT_VARIABLE: fake}), scan.EXIT_USAGE)

    def test_a_missing_tree_and_a_bad_argument_exit_2(self) -> None:
        self.assertEqual(self.run_main(["no-such-directory", "--hashes"]), scan.EXIT_USAGE)
        self.assertEqual(self.run_main([]), scan.EXIT_USAGE)
        self.assertEqual(self.run_main(["--unknown"]), scan.EXIT_USAGE)


if __name__ == "__main__":
    unittest.main()
