using System;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace Ink.Runtime
{
    /// <summary>
    /// Simple custom Choosatron serialisation implementation that takes JSON-able System.Collections that
    /// are produced by the ink engine and converts to a Choosatron Classic binary.
    /// </summary>
    public static class SimpleChoosatron
    {
        public class Writer
        {

            public Writer()
            {
                _writer = new BinaryWriter(new MemoryStream());
            }

            public Writer(Stream stream)
            {
                _writer = new BinaryWriter(stream, Encoding.UTF8);
            }

            public void WriterFlag()
            {

            }


            public MemoryStream Stream { get { return _writer.BaseStream as MemoryStream; } }


            public void WriteObject(Action<Writer> inner)
            {
                WriteObjectStart();
                inner(this);
                WriteObjectEnd();
            }

            public void WriteObjectStart()
            {
                //StartNewObject(container: true);
                _stateStack.Push(new StateElement { type = State.Object });
                //_writer.Write("{");
            }

            public void WriteObjectEnd()
            {
                Assert(state == State.Object);
                //_writer.Write("}");
                _stateStack.Pop();
            }

            public void WriteProperty(string name, Action<Writer> inner)
            {
                WriteProperty<string>(name, inner);
            }

            public void WriteProperty(int id, Action<Writer> inner)
            {
                WriteProperty<int>(id, inner);
            }

            public void WriteProperty(string name, string content)
            {
                WritePropertyStart(name);
                Write(content);
                WritePropertyEnd();
            }

            public void WriteProperty(string name, int content)
            {
                WritePropertyStart(name);
                Write(content);
                WritePropertyEnd();
            }

            public void WriteProperty(string name, bool content)
            {
                WritePropertyStart(name);
                Write(content);
                WritePropertyEnd();
            }

            public void WritePropertyStart(string name)
            {
                WritePropertyStart<string>(name);
            }

            public void WritePropertyStart(int id)
            {
                WritePropertyStart<int>(id);
            }

            public void WritePropertyEnd()
            {
                Assert(state == State.Property);
                Assert(childCount == 1);
                _stateStack.Pop();
            }

            public void WritePropertyNameStart()
            {
                Assert(state == State.Object);

                if (childCount > 0)
                    _writer.Write(",");

                _writer.Write("\"");

                IncrementChildCount();

                _stateStack.Push(new StateElement { type = State.Property });
                _stateStack.Push(new StateElement { type = State.PropertyName });
            }

            public void WritePropertyNameEnd()
            {
                Assert(state == State.PropertyName);

                _writer.Write("\":");

                // Pop PropertyName, leaving Property state
                _stateStack.Pop();
            }

            public void WritePropertyNameInner(string str)
            {
                Assert(state == State.PropertyName);
                _writer.Write(str);
            }

            void WritePropertyStart<T>(T name)
            {
                Assert(state == State.Object);

                if (childCount > 0)
                    _writer.Write(",");

                _writer.Write("\"");
                // TODO
                //_writer.Write(name);
                _writer.Write("\":");

                IncrementChildCount();

                _stateStack.Push(new StateElement { type = State.Property });
            }


            // allow name to be string or int
            void WriteProperty<T>(T name, Action<Writer> inner)
            {
                WritePropertyStart(name);

                inner(this);

                WritePropertyEnd();
            }

            public void WriteArrayStart()
            {
                StartNewObject(container: true);
                _stateStack.Push(new StateElement { type = State.Array });
                _writer.Write("[");
            }

            public void WriteArrayEnd()
            {
                Assert(state == State.Array);
                _writer.Write("]");
                _stateStack.Pop();
            }

            public void Write(byte b) {
                _writer.Write(b);
            }

            public void Write(byte[] b) {
                _writer.Write(b);
            }

            // public void Write(int i)
            // {
            //     _writer.Write(i);
            // }

            public void Write(Int16 i)
            {
                _writer.Write(i);
            }

            public void Write(UInt16 i)
            {
                _writer.Write(i);
            }

            public void Write(Int32 i)
            {
                _writer.Write(i);
            }

            public void Write(UInt32 i)
            {
                _writer.Write(i);
            }

            public void Write(Int64 i)
            {
                _writer.Write(i);
            }

            public void Write(UInt64 i)
            {
                _writer.Write(i);
            }

            public void Write(float f)
            {
                StartNewObject(container: false);

                // TODO: Find an heap-allocation-free way to do this please!
                // _writer.Write(formatStr, obj (the float)) requires boxing
                // Following implementation seems to work ok but requires creating temporary garbage string.
                string floatStr = f.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if( floatStr == "Infinity" ) {
                    _writer.Write("3.4E+38"); // JSON doesn't support, do our best alternative
                } else if (floatStr == "-Infinity") {
                    _writer.Write("-3.4E+38"); // JSON doesn't support, do our best alternative
                } else if ( floatStr == "NaN" ) {
                    _writer.Write("0.0"); // JSON doesn't support, not much we can do
                } else {
                    _writer.Write(floatStr);
                    if (!floatStr.Contains(".") && !floatStr.Contains("E")) 
                        _writer.Write(".0"); // ensure it gets read back in as a floating point value
                }
            }

            public void Write(string str, bool escape = true)
            {
                StartNewObject(container: false);

                _writer.Write("\"");
                if (escape)
                    WriteEscapedString(str);
                else
                    _writer.Write(str);
                _writer.Write("\"");
            }

            public void Write(bool b)
            {
                StartNewObject(container: false);
                _writer.Write(b ? "true" : "false");
            }

            public void WriteNull()
            {
                StartNewObject(container: false);
                _writer.Write("null");
            }

            public void WriteStringStart()
            {
                StartNewObject(container: false);
                _stateStack.Push(new StateElement { type = State.String });
                _writer.Write("\"");
            }

            public void WriteStringEnd()
            {
                Assert(state == State.String);
                _writer.Write("\"");
                _stateStack.Pop();
            }

            public void WriteStringInner(string str, bool escape = true)
            {
                Assert(state == State.String);
                if (escape)
                    WriteEscapedString(str);
                else
                    _writer.Write(str);
            }

            void WriteEscapedString(string str)
            {
                foreach (var c in str)
                {
                    if (c < ' ')
                    {
                        // Don't write any control characters except \n and \t
                        switch (c)
                        {
                            case '\n':
                                _writer.Write("\\n");
                                break;
                            case '\t':
                                _writer.Write("\\t");
                                break;
                        }
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\':
                            case '"':
                                _writer.Write("\\");
                                _writer.Write(c);
                                break;
                            default:
                                _writer.Write(c);
                                break;
                        }
                    }
                }
            }

            void StartNewObject(bool container)
            {

                // if (container)
                //     Assert(state == State.None || state == State.Property || state == State.Array);
                // else
                //     Assert(state == State.Property || state == State.Array);

                // if (state == State.Array && childCount > 0)
                //     _writer.Write(",");

                // if (state == State.Property)
                //     Assert(childCount == 0);

                // if (state == State.Array || state == State.Property)
                //     IncrementChildCount();
            }

            State state
            {
                get
                {
                    if (_stateStack.Count > 0) return _stateStack.Peek().type;
                    else return State.None;
                }
            }

            int childCount
            {
                get
                {
                    if (_stateStack.Count > 0) return _stateStack.Peek().childCount;
                    else return 0;
                }
            }

            void IncrementChildCount()
            {
                Assert(_stateStack.Count > 0);
                var currEl = _stateStack.Pop();
                currEl.childCount++;
                _stateStack.Push(currEl);
            }

            // Shouldn't hit this assert outside of initial development,
            // so it's save to make it debug-only.
            [System.Diagnostics.Conditional("DEBUG")]
            void Assert(bool condition)
            {
                if (!condition)
                    throw new System.Exception("Assert failed while writing CDAM");
            }

            public override string ToString()
            {
                return _writer.ToString();
            }

            enum State
            {
                None,
                Object,
                Array,
                Property,
                PropertyName,
                String
            };

            struct StateElement
            {
                public State type;
                public int childCount;
            }

            Stack<StateElement> _stateStack = new Stack<StateElement>();
            BinaryWriter _writer;
        }
    }
}