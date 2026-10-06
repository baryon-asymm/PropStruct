namespace PropStruct.Random;

/// <summary>The original's six generator streams (BOOT.md, Constraints: "Stream roles").</summary>
internal struct StreamSet
{
    public Mcg128State S1;
    public Mcg128State S2;
    public Mcg128State S3;
    public Mcg128State S4;
    public Mcg128State S5;
    public Mcg128State S6;
}
