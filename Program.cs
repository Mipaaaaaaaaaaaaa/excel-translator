﻿using CommandLine;
using ExcelTranslator.Excel;
using ExcelTranslator.Generator.Code;
using ExcelTranslator.Generator.Data;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace ExcelTranslator {
    public class Program {
        public static void Main(string[] args) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);
            parserResult.WithParsed(options => {
                /* 输出参数信息 */
                if (!string.IsNullOrEmpty(options.ExcelPath)) {
                    Console.WriteLine($"        [ExcelPath]: {options.ExcelPath}");
                }
                if (!string.IsNullOrEmpty(options.JSONPath)) {
                    Console.WriteLine($"        [JSON Path]: {options.JSONPath}");
                }
                if (!string.IsNullOrEmpty(options.CSharpCodePath)) {
                    Console.WriteLine($" [CSharp Code Path]: {options.CSharpCodePath}");
                }
                if (!string.IsNullOrEmpty(options.ClassNamespace)) {
                    Console.WriteLine($"  [Class Namespace]: {options.ClassNamespace}");
                }
                if (!string.IsNullOrEmpty(options.ClassNamePrefix)) {
                    Console.WriteLine($"[Class Name Prefix]: {options.ClassNamePrefix}");
                }
                if (!string.IsNullOrEmpty(options.EnumNamePrefix)) {
                    Console.WriteLine($" [Enum Name Prefix]: {options.EnumNamePrefix}");
                }
                if (!string.IsNullOrEmpty(options.ParamNamePrefix))
                {
                    Console.WriteLine($" [Enum Name Prefix]: {options.ParamNamePrefix}");
                }
                /* 开始转译数据 */
                DateTime startTime = DateTime.Now;
                Execute(options);
                TimeSpan during = DateTime.Now - startTime;
                Console.WriteLine("Conversion completed in {0} ms.", during.TotalMilliseconds);
            });
        }

        private static void Execute(Options options) {
            /* 判断 ExcelPath 为目录路径还是文件路径 */
            string[] excelPaths = null;
            if (Directory.Exists(options.ExcelPath)) {
                DirectoryInfo dirInfo = new DirectoryInfo(options.ExcelPath);
                excelPaths = dirInfo.GetFiles().Where(fileInfo => ExcelUtil.IsSupported(fileInfo.Name)).Select(fileInfo => fileInfo.FullName).ToArray();
            } else if (File.Exists(options.ExcelPath)) {
                if (ExcelUtil.IsSupported(options.ExcelPath)) {
                    excelPaths = new[] {options.ExcelPath};
                }
            }
            if (excelPaths == null) {
                Console.WriteLine("No supported excel file found.");
                return;
            }
            /* 对目标 Excel 文件进行转译 */
            foreach (var excelPath in excelPaths) {
                string excelName = Path.GetFileName(excelPath);
                Console.WriteLine("[{0}]", excelName);
                DataTableCollection dataTables = ExcelReader.ReadExcelToDataTables(excelPath);
                foreach (DataTable dataTable in dataTables) {
                    /* 开始转换 DataTable */
                    string sheetName = dataTable.TableName;
                    Console.WriteLine("  sheet {0}...", sheetName);
                    string fileName = ExcelUtil.IsEnumSheet(sheetName) ? options.EnumNamePrefix + sheetName.Substring(4) : ExcelUtil.IsParamSheet(sheetName) ? options.ParamNamePrefix + sheetName.Substring(5) : options.ClassNamePrefix + sheetName;
                    /* 生成 JSON 数据 */
                    Console.WriteLine("    generate json...");
                    string jsonContent = DataWriter.DataTableToJSON(dataTable, options);
                    if (!string.IsNullOrEmpty(jsonContent)) {
                        if (!Directory.Exists(options.JSONPath)) {
                            Directory.CreateDirectory(options.JSONPath);
                        }
                        string jsonPath = string.Format("{0}/{1}.json", options.JSONPath, fileName);
                        File.WriteAllText(jsonPath, jsonContent, Encoding.UTF8);
                    }
                    /* 生成 C# 代码 */
                    Console.WriteLine("    generate csharp code...");
                    string codeContent = CodeWriter.DataTableToCSharp(dataTable, excelName, options);
                    if (!string.IsNullOrEmpty(codeContent)) {
                        if (!Directory.Exists(options.CSharpCodePath)) {
                            Directory.CreateDirectory(options.CSharpCodePath);
                        }
                        string codePath = string.Format("{0}/{1}.cs", options.CSharpCodePath, fileName);
                        File.WriteAllText(codePath, codeContent, Encoding.UTF8);
                    }
                }
            }
        }
    }
}
