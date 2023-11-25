// Copyright (c) 2023 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace RhubarbGeekNz.PowerShellDataFile
{
    internal class TypeWriter
    {
        internal readonly Type Type;
        internal readonly Action<object, OutputBuffer, int> Writer;
        internal TypeWriter(Type t, Action<object, OutputBuffer, int> w)
        {
            Type = t;
            Writer = w;
        }
    }

    internal class OutputBuffer
    {
        StringBuilder Value;
        PSCmdlet Cmdlet;

        internal void Flush()
        {
            if (Value != null)
            {
                Cmdlet.WriteObject(Value.ToString());
                Value = null;
            }
        }

        void Append(string s, int tabs)
        {
            if (Value == null)
            {
                Value = new StringBuilder();
                while (tabs-- > 0)
                {
                    Value.Append("\t");
                }
            }

            Value.Append(s);
        }

        internal OutputBuffer(PSCmdlet cmdlet)
        {
            Cmdlet = cmdlet;
        }

        static readonly TypeWriter[] TypeWriters =
        {
            new TypeWriter(typeof(string),(o,e,t)=>{
                string s=(string)o;
                e.Append("'", t);
                if (s.Contains("\'")) s = s.Replace("\'","\'\'");
                e.Append(s, t);
                e.Append("'", t);
            }),
            new TypeWriter(typeof(bool),(o,e,t)=>{
                e.Append((bool)o ? "$True" : "$False", t);
            }),
            new TypeWriter(typeof(Int32),(o,e,t)=>{
                e.Append(o.ToString(), t);
            }),
            new TypeWriter(typeof(Double),(o,e,t)=>{
                e.Append(o.ToString(), t);
            }),
            new TypeWriter(typeof(Hashtable),(o,e,t)=>{
                e.Append("@{", t);
                e.Flush();
                foreach (DictionaryEntry i in (Hashtable)o)
                {
                    e.Append((string)i.Key, t+1);
                    e.Append(" = ", t+1);
                    e.WriteObject(i.Value,t+1);
                    e.Flush();
                }
                e.Append("}", t);
            }),
            new TypeWriter(typeof(object[]),(o,e,t)=>{
                object [] a= (object[])o;
                if (a.Length == 0)
                {
                    e.Append("@()", t);
                }
                else
                {
                    e.Append("@(", t);
                    e.Flush();
                    int j = 0;
                    foreach (var i in a)
                    {
                        e.WriteObject(i,t+1);
                        j++;
                        if (j < a.Length) e.Append(",", t+1);
                        e.Flush();
                    }
                    e.Append(")", t);
                }
            })
        };

        internal void WriteObject(object value, int tabs)
        {
            if (value == null)
            {
                Append("$null", tabs);
            }
            else
            {
                Type type = value.GetType();

                foreach (var typeWriter in TypeWriters)
                {
                    if (typeWriter.Type.IsAssignableFrom(type))
                    {
                        typeWriter.Writer(value, this, tabs);
                        return;
                    }
                }

                throw new InvalidDataException(type.FullName);
            }
        }
    }

    [Cmdlet("Export", "PowerShellDataFile")]
    [OutputType(typeof(string))]
    public class ExportPowerShellDataFile : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public Hashtable InputObject { get; set; }

        protected override void BeginProcessing()
        {
        }

        protected override void ProcessRecord()
        {
            OutputBuffer outputBuffer = new OutputBuffer(this);

            outputBuffer.WriteObject(InputObject, 0);

            outputBuffer.Flush();
        }

        protected override void EndProcessing()
        {
        }
    }
}
