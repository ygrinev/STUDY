using Microsoft.Extensions.Configuration;
using ParseLogFile.Model;
using ParseLogFile.Repo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParseLogFile
{
    class FileParser
    {
        readonly StreamReader _file = null;
        readonly IConfigurationRoot _config = null;
        readonly LogContext _context = null;

        public FileParser(IConfigurationRoot config, LogContext context)
        {
            _config = config;
            _context = context;
        }

        internal void Parse(string[] args) // async Task Async
        {
            List<string> list = FetchMatchingRows(_config.GetSection("rgxLines").Value, (args??new string[0]).Count() < 1 ? null : args[1]); // await 
            List<Dictionary<string,string>> data = DbDataToInsert(list);
            InsertRecords(data); // await Async
        }

        private void InsertRecords(List<Dictionary<string, string>> data) // async Task  Async
        {
            foreach(var row in data)
            {
                _context.Add(new InConnectionLog
                {
                    name = row["packID:name"].Split(_config.GetSection("splitColsVals").Value)[1],
                    srcIp = $"{row["ipFrom"]}:{row["portFrom"]}",
                    dstIp = $"{row["ipTo"]}:{row["portTo"]}",
                    dTime = DateTime.ParseExact($"{row["date"]} {row["time"]}", "MMM d yyyy h:m:s", null),
                    logId = int.Parse(row["logId"])
                });
            }
            _context.SaveChanges(); // await Async
        }

        public List<string> FetchMatchingRows(string regexPattern, string file = null) // async Task<> 
        {
            if(file == null)
            {
                file = Directory.GetFiles(Path.Combine( Directory.GetCurrentDirectory(), "Data"), "*.txt")[0];
            }
            List<string> list = new List<string>();
            Regex regex = new Regex(regexPattern);
            StreamReader reader = null;
            try
            {
                using (reader = string.IsNullOrEmpty((file??"").Trim()) ? _file : new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null) // await Async
                    {
                        // Try to match each line against the Regex.
                        Match match = regex.Match(line);
                        if (match.Success)
                        {
                            // Write original line into the output list
                            list.Add(line);
                            Console.WriteLine($"    {line}");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR: "+ex.Message);
            }
            finally
            {
                if(file == null && reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return list;
        }

        List<Dictionary<string, string>> DbDataToInsert(List<string> list)
        {
            List<string> format = _config.GetSection("cols").GetChildren().Select(s=>s.Value).ToList(); //  {"null","date", "time", "logId", "ipFrom", "portFrom", "ipTo", "portTo", @"packID:source:target|connection\s(d+)\s:([(].@[)])\sto inside:([(].@[)])$" };
            var rows = new List<Dictionary<string, string>>();

            foreach(var s in list)
            {
                var row = s.Split(_config.GetSection("splitCh").Value);
                rows.Add(format.Select((f, i) => GetColVal(f, row[i])).Where(p=>(p.Key??"").Trim().ToLower() != "null").ToDictionary(v=>v.Key, v=>v.Value));
            }
            return rows;
        }

        private KeyValuePair<string, string> GetColVal(string format, string src)
        {
            string [] cols = format.Split(_config.GetSection("splitColVal").Value);
            if(cols.Length == 1) // put as is
            {
                return new KeyValuePair<string, string>(cols[0], src);
            }
            else
            {
                List<string> lVals = new List<string>();
                string sDiv = _config.GetSection("splitColsVals").Value;
                foreach(string patt in cols[1].Split(sDiv))
                {
                    lVals.Add(Regex.Match(src, patt, RegexOptions.IgnoreCase).Value);
                }
                return new KeyValuePair<string, string>(cols[0], string.Join(sDiv, lVals));
            }
        }
    }
}
