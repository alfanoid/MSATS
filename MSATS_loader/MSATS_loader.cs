using StanMiscLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

class MSATS_loader
{
 
  public static string programName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

  private static Dictionary<string, string> Reports = new Dictionary<string, string>()
  {
    {"Level2SettlementReconciliation", "SettlementCase,TransmissionNodeIdentifier,LR,MDP"}
  };

  static void Main(string[] args)
  {

    int DebugLevel = 0;

    for( int i=0; i < args.Length; i++ )
    {
      StanMisc.Debug( i + ":" + args[i], 2);

      switch (args[i])
      {
        case "-d":
        case "--debuglevel":
               i++;
               if ( i >= args.Length )
                 Usage("Error: (-d) Provide a Debug Level");
               else
                 DebugLevel = Int32.Parse(args[i]);
               break;
        default:
               Usage("Error: Invalid Paramater(s)");
               break;
      }
    }

    StanMisc.DebugInit(DebugLevel);

    String dataDirectory = "data";

    string [] fileEntries = Directory.GetFiles(dataDirectory);

    
//    OracleConnection MANTPRO =  StanDB_DBOpen("mantpro");

    foreach(string fileName in fileEntries)
      ProcessFile(fileName);
  }

//=============== Functions =================


public static void ProcessFile(string fileName) 
{
  Console.WriteLine("====== {0} - Processing file '{1}'", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), fileName);     
  switch (Path.GetExtension(fileName).ToLower())
  {
    case ".xml":
         StanMisc.Debug("XML",1);
         ProcessXml(new FileStream(fileName, FileMode.Open, FileAccess.Read));
         break;
    case ".zip":
         StanMisc.Debug("ZIP",1);
         ProcessZipFile(ZipFile.OpenRead(fileName));
         break;
  }
  Console.WriteLine("****** {0} - Completed file '{1}'", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), fileName);
}


public static Dictionary<string, string> ProcessHeader(XmlReader Xml, Dictionary<string, string> IpIp) 
{
  string Key = "";
  string Val = "";

  while (Xml.Read())
  {
    if ( Xml.NodeType == XmlNodeType.Element)
    {
      Key = Xml.Name;
      Xml.Read();

      if ( Key == "MessageDate" )
        Val = XmlCnvDate(Xml.Value);
      else
        Val = Xml.Value;

      StanMisc.Debug(string.Format("Common:{0}:{1}", Key, Val), 11);
      IpIp.Add(Key, Val);
    }
    if ( Xml.NodeType == XmlNodeType.EndElement && Xml.Name == "Header" )
      break;
  }
  return IpIp;
}


public static void ProcessTransaction(XmlReader Xml, Dictionary<string, string> Common) 
{
  Dictionary<string, string> Transaction = new Dictionary<string, string>();

  if (Xml.MoveToFirstAttribute())
  {
    do
    {
      if ( Xml.Name == "transactionDate" )
        Transaction.Add(Xml.Name, XmlCnvDate(Xml.Value));
      else
        Transaction.Add(Xml.Name, Xml.Value);
    } while (Xml.MoveToNextAttribute());
  }

  while (Xml.Read() && ! (Xml.NodeType == XmlNodeType.EndElement && Xml.Name == "Transaction"))
  {
    switch (Xml.Name)
    {
      case "ReportResponse":
            if ( Xml.NodeType == XmlNodeType.EndElement )
              break;

            // Hold Transaction values for later use.
            Transaction.Add("Type", Xml.Name);
            StanMisc.Debug(string.Format("ReportResponse:{0}:{1}", Xml.Name, Xml.Value), 9);
            Xml.MoveToAttribute("version");
            Transaction.Add("version", Xml.Value.Replace("r",""));
            StanMisc.Debug(string.Format("Version:{0}", Transaction["version"]), 9);

            Dictionary<string, string> Report = new Dictionary<string, string>();

            // Get Report values for later use.
            Xml.ReadToFollowing("ReportParameters");
            while (Xml.Read() && ! (Xml.NodeType == XmlNodeType.EndElement && Xml.Name == "ReportParameters"))
            {
              if ( Xml.NodeType == XmlNodeType.Element )
              {
                string Key = Xml.Name;
                Xml.Read();
               // StanMisc.Debug(string.Format("{0}:{1}", Xml.Name, Xml.Value), 9);
                Report.Add(Key, Xml.Value);
              }
            }
  StanMisc_DumpDict("Transaction", Transaction, 9);
  StanMisc_DumpDict("Report", Report, 9);
            StanMisc.Exit();
            break;
    }
  }
  StanMisc_DumpDict("Transaction", Transaction, 9);
}


public static void ProcessXml1(Stream xmlStream) 
{
  XmlReaderSettings xSettings = new XmlReaderSettings();
  xSettings.IgnoreWhitespace = true;
//  XmlReader xReader = XmlReader.Create(xmlStream, new XmlReaderSettings(IgnoreWhitespace = true));
  XmlReader xReader = XmlReader.Create(xmlStream, xSettings);

  while (xReader.Read())
  {
//    xReader.ReadElementContentAsString("CSVData","");
//    if ( xReader.NodeType == XmlNodeType.Element)
//    {
//    xReader.ReadElementString(xReader.Name,"");
    StanMisc.Debug(string.Format("<{0}>{1}|{2}|", xReader.Name, xReader.NodeType, xReader.Value), 5);
 //   }
  }
}

public static void ProcessXml(Stream xmlStream) 
{
  Dictionary<string, string> Common = new Dictionary<string, string>();
                  
  XmlReaderSettings xSettings = new XmlReaderSettings();
  xSettings.IgnoreWhitespace = true;

  XmlReader xReader = XmlReader.Create(xmlStream, xSettings);

  while (xReader.Read())
  {
    switch (xReader.NodeType)
    {
      case XmlNodeType.Element:
           StanMisc.Debug(string.Format("XML Element:{0}:{1}", xReader.Name, xReader.Value), 11);
           switch (xReader.Name)
           {
             case "ase:aseXML":
                  xReader.MoveToAttribute("xmlns:ase");
                  Common.Add("aseXML", xReader.Value.Replace("urn:aseXML:r",""));
                  break;

             case "Header":
                  Common = ProcessHeader(xReader, Common);
                  StanMisc_DumpDict("Common", Common, 9);
                  break;

             case "Transaction":
                  ProcessTransaction(xReader, Common);
                  break;
           }
           break;
    }
  }
}



public static void ProcessXmlGood(Stream xmlStream) 
{
  XmlReader xReader = XmlReader.Create(xmlStream);

  while (xReader.Read())
  {
    switch (xReader.NodeType)
    {
      case XmlNodeType.Element:
           StanMisc.Debug("<" + xReader.Name + ">", 6);
           if (xReader.MoveToFirstAttribute())
           {
             StanMisc.Debug("Attr:", 7);
             do
             {
               string attributeName = xReader.Name;
               string attributeValue = xReader.Value;
               StanMisc.Debug(string.Format("{0}={1} ", attributeName, attributeValue), 5);
 
             } while (xReader.MoveToNextAttribute());
             StanMisc.Debug("", 5);
           }
           break;
      case XmlNodeType.Text:
           StanMisc.Debug(xReader.Value, 5);
           break;
      case XmlNodeType.EndElement:
           StanMisc.Debug("</" + xReader.Name + ">", 5);
           break;
    }
  }
}


public static void ProcessZipFile(ZipArchive Zip) 
{
    foreach (ZipArchiveEntry file in Zip.Entries)
    {
      StanMisc.Debug(string.Format("====== {0} - Processing file '{1}'", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), file.FullName), 10, "in");
      if (file.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
      {
        ProcessXml(file.Open());
      }
      StanMisc.Debug(string.Format("****** {0} - Completed file '{1}'", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), file.FullName), 10, "un");
    }
 // }
}


// Common Functions

public static void StanMisc_DumpDict(string DictName, Dictionary<string, string> ipip, int DbgLvl = 1)
{
  foreach( string s in ipip.Keys)
  {
    StanMisc.Debug("(" + DictName + ") " + s + '=' + ipip[s], DbgLvl);
  }
}


public static OracleConnection StanDB_DBOpen(string DBName, string User = "", string Password = "")
{
  if ( String.IsNullOrEmpty(DBName) )
  {
    StanMisc.PrintError("This is an error");
    StanMisc.Exit();
  }

  string conString = "";

  if ( User != "" )
  {
    if ( Password == "" )
      StanMisc.PrintError("Database Password not set for user(" + User + ")");
    conString = "Data Source=" + DBName + ";User Id=" + User + ";Password=" + Password;
  }
  switch (DBName.ToUpper())
  {
    case "INFOS":
         StanMisc.Debug(DBName.ToUpper(),7);
         conString = "Data Source=infos.stanwell.com;User Id=info_ro;Password=info_ro";
         break;
    case "MANTPRO":
         StanMisc.Debug(DBName.ToUpper(),7);
         conString = "Data Source=mantpro.stanwell.com;User Id=matuser;Password=matuser1";
         break;
    case "TRAPRO":
         StanMisc.Debug(DBName.ToUpper(),7);
         conString = "Data Source=trapro.stanwell.com;User Id=trader_read;Password=trader_read";
         break;
  }

  StanMisc.Debug("DBName:" + DBName, 1);
  StanMisc.Debug("conString:" + conString, 1);

  OracleConnection con =  new OracleConnection();
  con.ConnectionString = conString;
  try
  {
    con.Open();
  }
  catch (Exception OrrErr)
  {
    StanMisc.PrintError(OrrErr.Message.ToString());
  }

  return con;
}


public static void StanDB_DBFetch(OracleConnection DBCon)
{
      string SQLCommand = "select * from msats.nmis";

    OracleCommand OraCom = new OracleCommand(SQLCommand, DBCon);
    OracleDataReader OraComRead = OraCom.ExecuteReader();
    
    try
    {
      var rowValues = new object[OraComRead.FieldCount];


      while (OraComRead.Read())
      {
        Dictionary<string, string> MANTPROData = new Dictionary<string, string>();
        OraComRead.GetValues(rowValues);
        for (var keyValueCounter = 0; keyValueCounter < rowValues.Length; keyValueCounter++)
        {
          //MANTPROData.Add(OraComRead.GetName(keyValueCounter), rowValues[keyValueCounter]);
        }
        foreach( string s in MANTPROData.Keys)
        {
          StanMisc.Debug(s + '=' + MANTPROData[s], 1);
        }
      }
    }
    finally
    {
      // always call Close when done reading.
      OraComRead.Close();
    }
}


public static void Usage(string Message, int ExitCode = 0)
{
  string usage = "Usage: " + programName + " -d|--debuglevel DebugLevel";

  Trace.WriteLine( Message + "\n\n" + usage);
  StanMisc.Exit();
  
}

public static string XmlCnvDate(string Date)
{
  return Date.Replace("T"," ").Replace("+10:00","");
}


}
