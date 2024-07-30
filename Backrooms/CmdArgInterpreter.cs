using System;

namespace Backrooms;

public class CmdArgInterpreter
{
    public bool invalid;
    public int? screen;


    public CmdArgInterpreter(string[] args)
    {
        try
        {
            foreach(string arg in args)
                switch(arg)
                {
                    case var _ when arg.StartsWith("screen"):
                        screen = int.Parse(arg[^1].ToString());
                        break;
                }
        }
        catch(Exception exc)
        {
            invalid = true;
            OutErr(Log.Info, exc, "There was an error when trying to interpret command line arguments: $e");
        }
    }
}