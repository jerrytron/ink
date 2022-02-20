using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace Ink.Runtime
{
    public class ChoosatronPassage
    {
        public const byte kEndPsgByte = 0x03;
        public const int kAppendFlag = 7;
        public const int kContinueFlag = 6;
        byte _attributes = 0;
        List<ChoosatronOperation> _updateOps = new List<ChoosatronOperation>();
        string _body;
        List<ChoosatronChoice> _choices = new List<ChoosatronChoice>();

        public ChoosatronPassage() {

        }

        public void SetAppendFlag(bool aValue) {
            if (aValue) {
                Bits.SetBitTo1(_attributes, kAppendFlag);
            } else {
                Bits.SetBitTo0(_attributes, kAppendFlag);
            }
        }

        public void SetContinueFlag(bool aValue) {
            if (aValue) {
                Bits.SetBitTo1(_attributes, kContinueFlag);
            } else {
                Bits.SetBitTo0(_attributes, kContinueFlag);
            }
        }

        public void AddChoice() {

        }

        public byte[] ToBytes() {
            MemoryStream stream = new MemoryStream();
            SimpleChoosatron.Writer writer = new SimpleChoosatron.Writer( stream );

            // TODO: Convert to Choosatron Passage binary.

            return stream.ToArray();
        }
    }

    public class ChoosatronChoice
    {
        byte _attributes = 0;
        List<ChoosatronOperation> _conditionOps = new List<ChoosatronOperation>();
        List<ChoosatronOperation> _updateOps = new List<ChoosatronOperation>();
        string _body;
        string _psgLink;

        public ChoosatronChoice() {

        }

        public byte[] ToBytes() {
            MemoryStream stream = new MemoryStream();
            SimpleChoosatron.Writer writer = new SimpleChoosatron.Writer( stream );

            // TODO: Convert to Choosatron Passage binary.

            return stream.ToArray();
        }
    }

    public class ChoosatronOperation
    {
        byte _leftType;
        // If left type is operation this will be set.
        ChoosatronOperation _leftOp;
        byte _rightType;
        // If right type is operation this will be set.
        ChoosatronOperation _rightOp;
        byte _opType;

        public ChoosatronOperation() {

        }

        public byte[] ToBytes() {
            MemoryStream stream = new MemoryStream();
            SimpleChoosatron.Writer writer = new SimpleChoosatron.Writer( stream );

            // TODO: Convert to Choosatron Passage binary.

            return stream.ToArray();
        }
    }

    public static class Choosatron
    {
        public static void WriteBinary(SimpleChoosatron.Writer aWriter, Container aContainer) {
            //Console.WriteLine(aContainer.BuildStringOfHierarchy());

            // TODO: Iterate over everything FIRST to get the passage count, put together passages, and get their size. Then do the header.
            //List<Dictionary<string, ChoosatronPassage>> passages = new List<Dictionary<string, ChoosatronPassage>>();
            List<ChoosatronPassage> passages = new List<ChoosatronPassage>();
            List<UInt32> psgOffsets = new List<UInt32>();
            Dictionary<string, UInt16> psgToIdx = new Dictionary<string, UInt16>();
            Dictionary<string, UInt16> varToIdx = new Dictionary<string, UInt16>();
            ParseRuntimeContainer(aContainer);

            //ResolveReferences(passages, psgOffsets, psgToIdx, varToIdx);
            // TODO: Add to story size and var count.
            UInt32 storySize = 0;
            UInt16 varCount = 0;

            // Generate and write the story header - 414 bytes
            byte[] header = BuildHeader(aWriter, aContainer.content[0] as Container, storySize, varCount);
            aWriter.Write( header );

            // Passage Count - 2 bytes - Location 414 / 0x019E
            aWriter.Write((UInt16)passages.Count);

            // Passage Offsets - 4 bytes each
            foreach (UInt32 size in psgOffsets) {
                aWriter.Write(size);
            }

            // Passage Data
            foreach (ChoosatronPassage p in passages) {
                aWriter.Write(p.ToBytes());
            }
        }

        static void ParseRuntimeContainer(Container aContainer, bool aWithoutName = false) {
            
            foreach (var c in aContainer.content) {
                ParseRuntimeObject(c);
            }

            // Container is always an array [...]
            // But the final element is always either:
            //  - a dictionary containing the named content, as well as possibly
            //    the key "#" with the count flags
            //  - null, if neither of the above
            var namedOnlyContent = aContainer.namedOnlyContent;
            var countFlags = aContainer.countFlags;
            var hasNameProperty = aContainer.name != null && !aWithoutName;

            bool hasTerminator = namedOnlyContent != null || countFlags > 0 || hasNameProperty;

            if (hasTerminator) {

            }

            if ( namedOnlyContent != null ) {
                foreach (var namedContent in namedOnlyContent) {
                    var name = namedContent.Key;
                    var namedContainer = namedContent.Value as Container;
                    Console.WriteLine("Name: " + name);
                    ParseRuntimeContainer(namedContainer, aWithoutName:true);
                }
            }
        }

        static void ParseRuntimeObject(Runtime.Object aObj) {
            var container = aObj as Container;
            if (container) {
                ParseRuntimeContainer(container);
                return;
            }

            // A link - For Choosatron just about always a 'choice'.
            var divert = aObj as Divert;
            if (divert) {
                string divTypeKey = "->";
                if (divert.isExternal) {
                    // CDAM: Never to be supported (for external game-side function calls).
                    divTypeKey = "x()";
                } else if (divert.pushesToStack) {
                    if (divert.stackPushType == PushPopType.Function) {
                        // CDAM: Not supported.
                        divTypeKey = "f()";
                    } else if (divert.stackPushType == PushPopType.Tunnel) {
                        // CDAM: Not supported.
                        divTypeKey = "->t->";
                    }
                }

                string targetStr;
                if (divert.hasVariableTarget) {
                    targetStr = divert.variableDivertName;
                } else {
                    targetStr = divert.targetPathString;
                }

                Console.WriteLine("[Divert] " + divTypeKey + " | " + targetStr);
                //writer.WriteProperty(divTypeKey, targetStr);

                if (divert.hasVariableTarget) {
                    //writer.WriteProperty("var", true);
                }

                if (divert.isConditional) {
                    // TODO: Support passage conditional.
                    //writer.WriteProperty("c", true);
                }

                // CDAM: Never to be supported (for external game-side function calls).
                if (divert.externalArgs > 0) {
                    //writer.WriteProperty("exArgs", divert.externalArgs);
                }

                return;
            }

            var choicePoint = aObj as ChoicePoint;
            if (choicePoint) {
                Console.WriteLine("[ChoicePoint] * | " + choicePoint.pathStringOnChoice);
                Console.WriteLine("\t^ Flags: " + choicePoint.flags);
                //writer.WriteProperty("*", choicePoint.pathStringOnChoice);
                //writer.WriteProperty("flg", choicePoint.flags);
                return;
            }

            var strVal = aObj as StringValue;
            if (strVal) {
                if (strVal.isNewline) {
                    Console.WriteLine("\t^ Newline");
                    //writer.Write("\\n", escape:false);  
                } else {
                    Console.WriteLine("[String] " + strVal.value);
                    //writer.WriteStringInner("^");
                    //writer.WriteStringInner(strVal.value);
                }
                return;
            }

            // Used when serialising save state only
            var choice = aObj as Choice;
            if (choice) {
                Console.WriteLine("[Choice]");
                //WriteChoice(writer, choice);
                return;
            }
        }

        static byte[] BuildHeader(SimpleChoosatron.Writer aWriter, Container aContainer, UInt32 aStorySize, UInt16 aVarCount) {
            MemoryStream stream = new MemoryStream();
            SimpleChoosatron.Writer writer = new SimpleChoosatron.Writer( stream );

            // Start byte.
            writer.Write(kStartHeaderByte);
            WriterHeaderBinVersion(writer);
            
            // Parse all initial tags.
            Dictionary<string, string> tags = new Dictionary<string, string>();
            foreach (var obj in aContainer.content) {
                //WriteRuntimeObject(writer, c);

                // Tag
                var tag = obj as Tag;
                if (tag) {
                    string[] parts = tag.text.Split( ':' );
                    parts[1] = parts[1].Trim();
                    //Console.WriteLine( parts[0] + "-" + parts[1]);
                    tags.Add( parts[0], parts[1]);
                } else {
                    var divert = obj as Divert;
                    Console.WriteLine("Done with Tags");
                    break;
                }
            }

            if (!tags.ContainsKey( kStoryVersion )) {
                tags.Add( kStoryVersion, kStoryVersionDefault );
            }

            if (!tags.ContainsKey( kLanguageCode )) {
                tags.Add( kLanguageCode, kLanguageCodeDefault );
            }

            if (!tags.ContainsKey( kTitle )) {
                tags.Add( kTitle, kTitleDefault );
            }

            if (!tags.ContainsKey( kSubtitle )) {
                tags.Add( kSubtitle, kSubtitleDefault );
            }

            if (!tags.ContainsKey( kAuthor )) {
                tags.Add( kAuthor, kAuthorDefault );
            }

            if (!tags.ContainsKey( kCredits )) {
                tags.Add( kCredits, kCreditsDefault );
            }

            if (!tags.ContainsKey( kContact )) {
                tags.Add( kContact, kContactDefault );
            }

            // Either set or generate the IFID.
            if (tags.ContainsKey( kIfid )) {
                WriteHeaderIFID(writer, tags[kIfid], false);
            } else {
                // 'author + title' as seed to GUID.
                WriteHeaderIFID(writer, tags[kAuthor] + tags[kTitle]);
            }
            
            // Set story flags - 4 bytes (16 flags)
            // TODO: Set flags as tags.
            byte f = 0;
            f = (byte)Bits.SetBit(f, 4, true);
            Console.WriteLine("Flag 1: " + Bits.GetBinaryString(f));
            writer.Write(f);
            f = 0;
            f = (byte)Bits.SetBit(f, 7, true);
            Console.WriteLine("Flag 2: " + Bits.GetBinaryString(f));
            writer.Write(f);
            f = 0;
            writer.Write(f);
            writer.Write(f);
            //writer.Stream.Position += 4;

            // Story size - 4 bytes - Location 44 / 0x2C
            writer.Write(aStorySize);
            //writer.Stream.Position += 4;

            // Story version - 3 bytes (1 Major, 1 Minor, 1 Revision)
            string[] version = tags[kStoryVersion].Split( '.' );
            byte.TryParse( version[0], out byte major );
            byte.TryParse( version[1], out byte minor );
            byte.TryParse( version[2], out byte rev );
            writer.Write(major);
            writer.Write(minor);
            writer.Write(rev);

            // Reserved byte.
            writer.Stream.Position++;

            // Use for properly sized byte buffers.
            byte[] data;
            byte[] buffer;

            // Lanuage Code - 4 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kLanguageCode] );
            buffer = new byte[4];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            writer.Write( buffer );

            // Title - 64 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kTitle] );
            buffer = new byte[64];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            writer.Write( buffer );

            // Subtitle - 32 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kSubtitle] );
            buffer = new byte[32];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            writer.Write( buffer );

            // Author - 48 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kAuthor] );
            buffer = new byte[48];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            writer.Write( buffer );

            // Credits - 80 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kCredits] );
            buffer = new byte[80];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            writer.Write( buffer );

            // Contact - 128 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kContact] );
            buffer = new byte[128];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            writer.Write( buffer );

            // Publish Date - 4 bytes
            long published;
            DateTimeOffset dto;
            if (tags.ContainsKey( kPublishDate )) {
                DateTime dt;
                try {
                    dt = DateTime.Parse(tags[kPublishDate]);
                } catch (FormatException) {
                    throw new System.Exception("Unable to convert 'publish' tag to datetime '" + tags[kPublishDate] + "'.");
                }
                dto = new DateTimeOffset(DateTime.Parse(tags[kPublishDate]));
            } else {
                dto = new DateTimeOffset(DateTime.Now);
            }
            //DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
            published = dto.ToUnixTimeSeconds();
            writer.Write((UInt32)published);
            Console.WriteLine("Publish Time in Unix Epoch Seconds: " + published);
             
            // Variable Count - 2 bytes - Location 412 / 0x019C
            writer.Write(aVarCount);

            // Passage Count - 2 bytes - Location 414 / 0x019E
            // UInt16 psgCount = 0;
            // writer.Write(psgCount);

            return stream.ToArray();
        }

        static void WriterHeaderBinVersion(SimpleChoosatron.Writer aWriter) {
            // Version of the Choosatron binary.
            aWriter.Write(kBinVerMajor);
            aWriter.Write(kBinVerMinor);
            aWriter.Write(kBinVerRev);
        }

        static void WriteHeaderIFID(SimpleChoosatron.Writer aWriter, string aValue, bool aIsSeed = true) {
            string hexHash = aValue;
            if (aIsSeed) {
                
                MD5 md5 = MD5.Create();
                // Use the seed to create a hash.
                byte[] data = md5.ComputeHash(Encoding.ASCII.GetBytes(aValue));
                // TODO: This is more complicated because of how it is being done for
                // the Python Twine stuff. We are create a GUID, then converting the hex
                // values to string and writing those bytes. Ideally just ToString the
                // data directly.
                hexHash = "";
                foreach (byte b in data) {
                    hexHash += String.Format("{0:x2}", b);
                }
            }
            Guid ifid;
            try {
                ifid = new Guid(hexHash);
            } catch (System.Exception e) {
                if (!aIsSeed) {
                    throw new System.Exception(e.Message + " Invalid 'ifid' tag provided.");
                } else {
                    throw new System.Exception(e.Message);
                }
            }
            string ifidStr = ifid.ToString("D").ToUpper();
            aWriter.Write(Encoding.ASCII.GetBytes(ifidStr));
            Console.WriteLine("IFID: " + ifidStr + ", Len: " + ifidStr.Length);
        }

        static Choosatron() {
            _controlCommandNames = new string[(int)ControlCommand.CommandType.TOTAL_VALUES];

            _controlCommandNames [(int)ControlCommand.CommandType.EvalStart] = "ev";
            _controlCommandNames [(int)ControlCommand.CommandType.EvalOutput] = "out";
            _controlCommandNames [(int)ControlCommand.CommandType.EvalEnd] = "/ev";
            _controlCommandNames [(int)ControlCommand.CommandType.Duplicate] = "du";
            _controlCommandNames [(int)ControlCommand.CommandType.PopEvaluatedValue] = "pop";
            _controlCommandNames [(int)ControlCommand.CommandType.PopFunction] = "~ret";
            _controlCommandNames [(int)ControlCommand.CommandType.PopTunnel] = "->->";
            _controlCommandNames [(int)ControlCommand.CommandType.BeginString] = "str";
            _controlCommandNames [(int)ControlCommand.CommandType.EndString] = "/str";
            _controlCommandNames [(int)ControlCommand.CommandType.NoOp] = "nop";
            _controlCommandNames [(int)ControlCommand.CommandType.ChoiceCount] = "choiceCnt";
            _controlCommandNames [(int)ControlCommand.CommandType.Turns] = "turn";
            _controlCommandNames [(int)ControlCommand.CommandType.TurnsSince] = "turns";
            _controlCommandNames [(int)ControlCommand.CommandType.ReadCount] = "readc";
            _controlCommandNames [(int)ControlCommand.CommandType.Random] = "rnd";
            _controlCommandNames [(int)ControlCommand.CommandType.SeedRandom] = "srnd";
            _controlCommandNames [(int)ControlCommand.CommandType.VisitIndex] = "visit";
            _controlCommandNames [(int)ControlCommand.CommandType.SequenceShuffleIndex] = "seq";
            _controlCommandNames [(int)ControlCommand.CommandType.StartThread] = "thread";
            _controlCommandNames [(int)ControlCommand.CommandType.Done] = "done";
            _controlCommandNames [(int)ControlCommand.CommandType.End] = "end";
            _controlCommandNames [(int)ControlCommand.CommandType.ListFromInt] = "listInt";
            _controlCommandNames [(int)ControlCommand.CommandType.ListRange] = "range";
            _controlCommandNames [(int)ControlCommand.CommandType.ListRandom] = "lrnd";

            for (int i = 0; i < (int)ControlCommand.CommandType.TOTAL_VALUES; ++i) {
                  if (_controlCommandNames [i] == null)
                     throw new System.Exception ("Control command not accounted for in serialisation");
            }
        }

        static string[] _controlCommandNames;


        public const byte kStartHeaderByte = 0x01;
        public const byte kBinVerMajor = 1;
        public const byte kBinVerMinor = 0;
        public const byte kBinVerRev = 6;

        public const string kIfid = "ifid";
        public const string kStoryVersion = "version";
        public const string kStoryVersionDefault = "1.0.0";
        public const string kLanguageCode = "language";
        public const string kLanguageCodeDefault = "eng";
        public const string kTitle = "title";
        public const string kTitleDefault = "untitled";
        public const string kSubtitle = "subtitle";
        public const string kSubtitleDefault = "";
        public const string kAuthor = "author";
        public const string kAuthorDefault = "anonymous";
        public const string kCredits = "credits";
        public const string kCreditsDefault = "";
        public const string kContact = "contact";
        public const string kContactDefault = "";
        public const string kPublishDate = "published";

    }

    static class Bits
    {
        public static int SetBitTo1(this int value, int position) {
            // Set a bit at position to 1.
            return value |= (1 << position);
        }

        public static int SetBitTo0(this int value, int position) {
            // Set a bit at position to 0.
            return value & ~(1 << position);
        }

        // Position is index from right to left (0 is far right position).
        public static int SetBit(this int value, int position, bool state) {
            if (state) {
                // Set a bit at position to 1.
                return value |= (1 << position);   
            }
            // Set a bit at position to 0.
            return value & ~(1 << position);
        }

        public static bool IsBitSetTo1(this int value, int position) {
            // Return whether bit at position is set to 1.
            return (value & (1 << position)) != 0;
        }

        public static bool IsBitSetTo0(this int value, int position) {
            // If not 1, bit is 0.
            return !IsBitSetTo1(value, position);
        }
        public static string GetBinaryString(byte n) {
            char[] b = new char[8];
            int pos = 7;
            int i = 0;
        
            while (i < 8) {
                if ((n & (1 << i)) != 0)
                    b[pos] = '1';
                else
                    b[pos] = '0';
                pos--;
                i++;
            }
            return new string(b);
        }
    }
}