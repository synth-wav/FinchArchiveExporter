using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace FinchArchiveExporter {
    class ParseArchive {
        static void Main(string[] args) {
            String fileName;
            List<Entry> entries = new List<Entry>();
            entries.Sort((x, y) => DateTime.Compare(x.entryTime, y.entryTime));
            if (args.Length == 0 || args.Length > 1) {
                Console.WriteLine("Usage: FinchArchiveParser inputFile.json");
            }
            String line = "";
            fileName = args[0];
            try {
                line = File.ReadLines(fileName).First();
                //Console.WriteLine(line);
            }
            catch (Exception e) {
                Console.WriteLine("Error opening file {0}", args[0]);
                Console.WriteLine(e.Message);
            }
            int nextBulletPos = line.IndexOf("\"bullet_type\":2");
            String lineTrimmed = "";
            if (nextBulletPos > 0) {
                lineTrimmed = line.Substring(nextBulletPos);
            }
            else
            {
                Console.WriteLine("The file does not contain any journal entries");
                System.Environment.Exit(1);
            }
            while(nextBulletPos > 0) {
                String entryBody = lineTrimmed.Substring(24, (lineTrimmed.IndexOf("\",") - 24));
                entryBody = entryBody.Replace("#","");
                entryBody = entryBody.Replace("_", "");
                //Console.WriteLine("Text body is: {0}", entryBody);
                String entryTimeString = lineTrimmed.Substring(lineTrimmed.IndexOf("creation_time") + 21,  20);
                entryTimeString = entryTimeString.Replace("\"", "");
                //Console.WriteLine("Time string is {0}", entryTimeString);
                DateTime entryTime = DateTime.Parse(entryTimeString);
                //Console.WriteLine("DateTime is {0}", entryTime.ToString());
                Entry currentEntry = new Entry(entryTime, entryBody);
                entries.Add(currentEntry);
                line = lineTrimmed.Substring(line.IndexOf("creation_time") + 43);
                nextBulletPos = line.IndexOf("\"bullet_type\":2");
                if(nextBulletPos > 0)
                    lineTrimmed = line.Substring(nextBulletPos);
            }

            entries.Sort();
            using (FileStream fs = File.Create("FinchArchiveExport" + DateTime.Now.ToString("MM-dd-yyyy-hh-mm-ss") + ".txt"))
            {
                using (var fw = new StreamWriter(fs))
                {
                    fw.WriteLine("Finch Journal Entry Export " + DateTime.Now.ToString("D"));
                    fw.WriteLine("-------------------------------------------------------------------\n");
                    DateTime currentDate = entries[0].entryTime.Date.AddDays(-1);
                    Console.WriteLine(currentDate.ToString());
                    fw.WriteLine(currentDate.ToString("D") + "\n");
                    foreach (Entry entry in entries)
                    {
                        if (entry.entryTime.Date > currentDate)
                        {
                            currentDate = entry.entryTime.Date.AddDays(-1);
                            fw.WriteLine("\n" + currentDate.ToString("D") + "\n");
                            Console.WriteLine("\n" + currentDate.ToString("D") + "\n");
                        }
                        fw.WriteLine("{0}: {1}", entry.entryTime.ToShortTimeString(), entry.entryBody);
                        Console.WriteLine("{0}: {1}", entry.entryTime.ToShortTimeString(), entry.entryBody);
                    }
                }
            }
            System.Environment.Exit(0);
        }
    }

    class Entry : IComparable<Entry> {
        public DateTime entryTime;
        public String entryBody;

        public Entry(DateTime myEntryTime, String myEntryBody) {
            this.entryTime = myEntryTime;
            this.entryBody = myEntryBody;
        }

        int IComparable<Entry>.CompareTo(Entry other)
        {
            return DateTime.Compare(this.entryTime, other.entryTime);
        }
    }
}
