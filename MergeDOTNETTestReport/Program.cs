﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MergeDOTNETTestReport
{
    class Program
    {
        public const string PARAM_INPUTPATH     = "-inputpath";
        public const string PARAM_OUTPUTPATH    = "-outputpath";
        public const string PARAM_REPORTNAME    = "-reportname";

        private static int TotalPass = 0;
        private static int TotalFail = 0;
        private static int TotalSkip = 0;

        /// <summary>
        /// -inputpath
        /// -outputpath
        /// -reportname
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Check if any arguments are passed
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided more info use -h");
                return;
            }

            if (args.Contains("-v"))
            {
                // Get Application Version from Assembly
                Console.WriteLine("Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                return;
            }

            if (args.Contains("-h"))
            {
                Console.WriteLine("-h for help");
                Console.WriteLine("-v for get version");
                Console.WriteLine("Sample usage");
                Console.WriteLine("MergeDOTNETTestReport -inputpath <path_to_html_dotnet_test_report> -outputpath <path> -reportname <name>.html");
                return;
            }

            HtmlDocument doc = new HtmlDocument();
            HtmlNode htmlNode = HtmlNode.CreateNode("<html>");
            HtmlNode bodyNode = HtmlNode.CreateNode("<body>");
            HtmlNode testDetailNode = HtmlNode.CreateNode("<div>");
            IDictionary<string, string> inputDic = convertInputDic(args);

            if (!inputDic.ContainsKey(PARAM_INPUTPATH) || !inputDic.ContainsKey(PARAM_OUTPUTPATH) || !inputDic.ContainsKey(PARAM_REPORTNAME))
            {
                Console.WriteLine("Invalid input parameters for MergeDOTNETTestReport");
                return;
            }

            if (!Directory.Exists(inputDic[PARAM_INPUTPATH]))
            {
                Console.WriteLine("Input path does not exist");
                return;
            }

            if (!Directory.Exists(inputDic[PARAM_OUTPUTPATH]))
            {
                Console.WriteLine("Output path does not exist");
                return;
            }

            String finalPath = inputDic[PARAM_OUTPUTPATH] + "\\" + inputDic[PARAM_REPORTNAME];
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            IEnumerable<String> testReportFiles = getTestReportInPath(inputDic[PARAM_INPUTPATH]);

            if (!testReportFiles.Any())
            {
                Console.WriteLine("Not found any dotnet html test report");
                return;
            }

            HtmlNode scriptNode = getScriptSection(testReportFiles.First());
            HtmlNode styleNode = getStyleSection(testReportFiles.First());
            if (scriptNode != null)
            {
                htmlNode.AppendChild(scriptNode);
            }

            if (styleNode != null)
            {
                htmlNode.AppendChild(styleNode);
            }
            
            foreach (string testReportFile in testReportFiles)
            {
                HtmlNode testReportSummrary = getSummarySection(testReportFile);
                if (testReportSummrary != null)
                {
                    testDetailNode.AppendChild(testReportSummrary);
                }
            }

            String rawSummaryHtml = @" <div id=""TestSummary"" class=""summary"">
                                        </br>
                                        <div class=""block"">
                                            <h1>Total Test</h1>
                                            <span>Passed  : </span>
                                            <span class=""passedTests"">{0}</span></br>
                                            <span>Failed  : </span>
                                            <span class=""failedTests"">{1}</span></br>
                                            <span>Skipped : </span>
                                            <span class=""skippedTests"">{2}</span></br>
                                        </div>
                                       </div>";
            rawSummaryHtml = String.Format(rawSummaryHtml, TotalPass, TotalFail, TotalSkip);
            HtmlNode testSummaryNode = HtmlNode.CreateNode(rawSummaryHtml);


            bodyNode.ChildNodes.Add(testSummaryNode);
            bodyNode.ChildNodes.Add(testDetailNode);

            htmlNode.AppendChild(bodyNode);


            doc.DocumentNode.AppendChild(htmlNode);



            TextWriter writer = File.CreateText(finalPath);
            doc.Save(writer);
        }

        private static IDictionary<string, string> convertInputDic(string[] pParam)
        {
            IDictionary<string, string> inputDic = new Dictionary<String, String>();

            for (int i = 0; i < pParam.Length; i=i+2)
            {
                inputDic.Add(pParam[i], pParam[i+1]);
            }
            return inputDic;
        }

        private static IEnumerable<String> getTestReportInPath(string pBasePath)
        {
            IList<String> ext = new List<string> { "htm", "html" };
            IEnumerable<String> testReportFiles = Directory.EnumerateFiles(pBasePath, "*.*", SearchOption.AllDirectories)
                                                           .Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));

            return testReportFiles;
        }

        private static HtmlNode getSummarySection(string pPath)
        {
            string rawHTML = File.ReadAllText(pPath);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHTML);

            HtmlNodeCollection summary = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'summary')]");
            HtmlNodeCollection PassTest = summary.Select(c1 => c1.SelectNodes("//span[contains(@class, 'passedTests')]")).First();
            TotalPass = TotalPass + Convert.ToInt32(PassTest[0].InnerHtml);

            HtmlNodeCollection FailTest = summary.Select(c1 => c1.SelectNodes("//span[contains(@class, 'failedTests')]")).First();
            TotalFail = TotalFail + Convert.ToInt32(FailTest[0].InnerHtml);

            HtmlNodeCollection SkipTest = summary.Select(c1 => c1.SelectNodes("//span[contains(@class, 'skippedTests')]")).First();
            TotalSkip = TotalSkip + Convert.ToInt32(SkipTest[0].InnerHtml);

            //TotalF
            HtmlNodeCollection testFilels = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'list-row')]");
            String testDLLName = "Dummy";
            if (testFilels != null)
            {  
                foreach (HtmlNode testfile in testFilels)
                {
                    String value = testfile.InnerText;
                    if (value.Contains(".dll"))
                    {
                        //Create new tag
                        testDLLName = Path.GetFileName(value);
                        break;
                    }
                }
            }
            else
            {
                testDLLName=Path.GetFileName(pPath);
            }
            HtmlNode dllTestSummary = htmlDoc.CreateElement("div");
            HtmlNode h2Node = HtmlNode.CreateNode("<h2> Test Run: " + testDLLName + " </h2>");
            dllTestSummary.AppendChild(h2Node);

            dllTestSummary.AppendChild(summary[0]);

            return dllTestSummary;
        }

        private static HtmlNode getScriptSection(string pPath)
        {
            string rawHTML = File.ReadAllText(pPath);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHTML);

            HtmlNodeCollection scriptNode = htmlDoc.DocumentNode.SelectNodes("//script");
            if (scriptNode == null)
            {
                return null;
            }
            return scriptNode[0];

        }

        private static HtmlNode getStyleSection(string pPath)
        {
            string rawHTML = File.ReadAllText(pPath);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHTML);

            HtmlNodeCollection styleNode = htmlDoc.DocumentNode.SelectNodes("//style");
            if (styleNode == null)
            {
                return null;
            }
            return styleNode[0];

        }
    }
}
