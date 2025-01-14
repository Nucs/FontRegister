using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace FontRegister.UnitTests;

public class TestConsts
{
    public static string GetTestPath()
    {
        var testName = TestContext.CurrentContext.Test.FullName;
        testName = string.Join("", testName.Split(Path.GetInvalidFileNameChars())).Replace(".", "_").Replace(",", "_");
        testName = Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(testName)));
        testName = string.Join("", testName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(Path.GetTempPath(), "TestFonts_" + testName + "_" + Guid.NewGuid().ToString("N"));
    }

    public static string GetTestFontFilePath(string tempDirectory, string originalFileName)
    {
        string fontPath = Path.Combine(tempDirectory, GetTestFontFileName(originalFileName, Guid.NewGuid().ToString("N").Substring(0, 16)));
        return fontPath;
    }

    public static string GetTestFontFileName(string originalFileName, string randomStr = "", bool withExtension = true)
    {
        var testName = TestContext.CurrentContext.Test.FullName;
        testName = string.Join("", testName.Split(Path.GetInvalidFileNameChars())).Replace(".", "_").Replace(",", "_");
        testName = Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(testName)));
        testName = string.Join("", testName.Split(Path.GetInvalidFileNameChars()));

        string fontName = $"TestFont_{testName}" + (string.IsNullOrEmpty(randomStr) ? "" : $"_{randomStr}");
        return $"{fontName}" +
               (withExtension ? $"{Path.GetExtension(originalFileName)}" : "");
    }
}