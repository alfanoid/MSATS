using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace StanMiscLib
{
  public class StanMisc

  {
    public static int GlobalDebugLevel = 0; 

    public static void Exit(int exitCode = 0)
    {
      System.Environment.Exit(exitCode);
    }

    public static void DebugInit(int DebugLevel = 0)
    {
      Trace.Listeners.Add(new TextWriterTraceListener(Console.Error));
      Trace.AutoFlush = true;
      GlobalDebugLevel = DebugLevel;
    }

    public static void Debug(string Message, int DbgLvl, string Indent = "")
    {
      if ( DbgLvl <= GlobalDebugLevel )
      {
        StackFrame callStack = new StackFrame(1, true);

        if ( Indent.ToLower() == "un" )
            Trace.Unindent();

        Trace.WriteLine( "Debug(" + callStack.GetFileLineNumber() + "):" + Message );

        if ( Indent.ToLower() == "in" ) 
            Trace.Indent();

      }
    }

    public static void PrintError(string Message, int ExitCode = 1)
    {
      StackFrame callStack = new StackFrame(1, true);
      Trace.WriteLine( "ERROR: " + Message + " at line " + callStack.GetFileLineNumber() + " (" + callStack.GetFileName() + ")");
      StanMisc.Exit(ExitCode);
    }

  }
}

