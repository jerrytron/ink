using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace Ink.Runtime
{
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

     public static class Choosatron
     {
        public static void WriteBinary(SimpleChoosatron.Writer aWriter, Container aContainer)
        {
            //Console.WriteLine(aContainer.BuildStringOfHierarchy());
            BuildHeader(aWriter, aContainer.content[0] as Container);
        }

        static void BuildHeader(SimpleChoosatron.Writer aWriter, Container aContainer)
        {
            // byte[] header = new byte[414]; // Size of header in bytes.
            // MemoryStream memStream = new MemoryStream( header );
            // BinaryWriter binWriter = new BinaryWriter( memStream );

            // Start byte.
            aWriter.Write(kStartHeaderByte);
            WriterHeaderBinVersion(aWriter);
            
            // Parse all initial tags.
            Dictionary<string, string> tags = new Dictionary<string, string>();
            foreach (var c in aContainer.content) {
                //WriteRuntimeObject(aWriter, c);

                // Tag
                var tag = c as Tag;
                if (tag) {
                    string[] parts = tag.text.Split( ':' );
                    parts[1] = parts[1].Trim();
                    //Console.WriteLine( parts[0] + "-" + parts[1]);
                    tags.Add( parts[0], parts[1]);
                } else {
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
                WriteHeaderIFID(aWriter, tags[kIfid], false);
            } else {
                // 'author + title' as seed to GUID.
                WriteHeaderIFID(aWriter, tags[kAuthor] + tags[kTitle]);
            }
            
            // Set story flags - 4 bytes (16 flags)
            // TODO: Set flags as tags.
            byte f = 0;
            f = (byte)Bits.SetBit(f, 4, true);
            Console.WriteLine(Bits.GetBinaryString(f));
            aWriter.Write(f);
            f = 0;
            f = (byte)Bits.SetBit(f, 7, true);
            Console.WriteLine(Bits.GetBinaryString(f));
            aWriter.Write(f);
            f = 0;
            aWriter.Write(f);
            aWriter.Write(f);
            //aWriter.Stream.Position += 4;

            // Story size - 4 bytes (unknown at this stage) - Location 44 / 0x2C
            UInt32 size = 0;
            aWriter.Write(size);
            //aWriter.Stream.Position += 4;

            // Story version - 3 bytes (1 Major, 1 Minor, 1 Revision)
            string[] version = tags[kStoryVersion].Split( '.' );
            byte.TryParse( version[0], out byte major );
            byte.TryParse( version[1], out byte minor );
            byte.TryParse( version[2], out byte rev );
            aWriter.Write(major);
            aWriter.Write(minor);
            aWriter.Write(rev);

            // Reserved byte.
            aWriter.Stream.Position++;

            // Use for properly sized byte buffers.
            byte[] data;
            byte[] buffer;

            // Lanuage Code - 4 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kLanguageCode] );
            buffer = new byte[4];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            aWriter.Write( buffer );

            // Title - 64 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kTitle] );
            buffer = new byte[64];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            aWriter.Write( buffer );

            // Subtitle - 32 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kSubtitle] );
            buffer = new byte[32];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            aWriter.Write( buffer );

            // Author - 48 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kAuthor] );
            buffer = new byte[48];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            aWriter.Write( buffer );

            // Credits - 80 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kCredits] );
            buffer = new byte[80];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            aWriter.Write( buffer );

            // Contact - 128 bytes
            data = ASCIIEncoding.ASCII.GetBytes( tags[kContact] );
            buffer = new byte[128];
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
            aWriter.Write( buffer );

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
            aWriter.Write((UInt32)published);
            Console.WriteLine(published);
             
            // Variable Count - 2 bytes - Location 412 / 0x019C
            UInt16 varCount = 0;
            aWriter.Write(varCount);

            // Passage Count - 2 bytes - Location 414 / 0x019E
            UInt16 psgCount = 0;
            aWriter.Write(psgCount);
        }

        static void WriterHeaderBinVersion(SimpleChoosatron.Writer aWriter)
        {
            // Version of the Choosatron binary.
            aWriter.Write(kBinVerMajor);
            aWriter.Write(kBinVerMinor);
            aWriter.Write(kBinVerRev);
        }

        static void WriteHeaderIFID(SimpleChoosatron.Writer aWriter, string aValue, bool aIsSeed = true)
        {
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

        public static void ParseWhileTags(Container container) {

        }

        public static void WriteRuntimeContainer(SimpleChoosatron.Writer writer, Container container, bool withoutName = false)
        {
            //writer.WriteArrayStart();

            Console.WriteLine("Name: " + container.name);
            // Console.WriteLine(container.BuildStringOfHierarchy());

            // foreach (var c in container.content) {
            //     WriteRuntimeObject(writer, c);
            // }

            // Container is always an array [...]
            // But the final element is always either:
            //  - a dictionary containing the named content, as well as possibly
            //    the key "#" with the count flags
            //  - null, if neither of the above
            var namedOnlyContent = container.namedOnlyContent;
            var countFlags = container.countFlags;
            var hasNameProperty = container.name != null && !withoutName;

            bool hasTerminator = namedOnlyContent != null || countFlags > 0 || hasNameProperty;

            // if( hasTerminator )
            //     writer.WriteObjectStart();

            if ( namedOnlyContent != null ) {
                foreach(var namedContent in namedOnlyContent) {
                    var name = namedContent.Key;
                    var namedContainer = namedContent.Value as Container;
                    Console.WriteLine("NamedContent: " + name);
                    //   writer.WritePropertyStart(name);
                    //   WriteRuntimeContainer(writer, namedContainer, withoutName:true);
                    //   writer.WritePropertyEnd();
                }
            }

            // if (countFlags > 0)
            //     writer.WriteProperty("#f", countFlags);

            // if (hasNameProperty)
            //     writer.WriteProperty("#n", container.name);

            // if (hasTerminator)
            //     writer.WriteObjectEnd();
            // else
            //     writer.WriteNull();

            // writer.WriteArrayEnd();
        }

        public static void WriteRuntimeObject(SimpleChoosatron.Writer writer, Runtime.Object obj)
        {
            var container = obj as Container;
            if (container) {
                WriteRuntimeContainer(writer, container);
                return;
            }

            var divert = obj as Divert;
            if (divert)
            {
                string divTypeKey = "->";
                if (divert.isExternal)
                    divTypeKey = "x()";
                else if (divert.pushesToStack)
                {
                    if (divert.stackPushType == PushPopType.Function)
                        divTypeKey = "f()";
                    else if (divert.stackPushType == PushPopType.Tunnel)
                        divTypeKey = "->t->";
                }

                string targetStr;
                if (divert.hasVariableTarget)
                    targetStr = divert.variableDivertName;
                else
                    targetStr = divert.targetPathString;

                writer.WriteObjectStart();

                writer.WriteProperty(divTypeKey, targetStr);

                if (divert.hasVariableTarget)
                    writer.WriteProperty("var", true);

                if (divert.isConditional)
                    writer.WriteProperty("c", true);

                if (divert.externalArgs > 0)
                    writer.WriteProperty("exArgs", divert.externalArgs);

                writer.WriteObjectEnd();
                return;
            }

            var choicePoint = obj as ChoicePoint;
            if (choicePoint)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("*", choicePoint.pathStringOnChoice);
                writer.WriteProperty("flg", choicePoint.flags);
                writer.WriteObjectEnd();
                return;
            }

            var boolVal = obj as BoolValue;
            if (boolVal) {
                writer.Write(boolVal.value);
                return;
            }

            var intVal = obj as IntValue;
            if (intVal) {
                writer.Write(intVal.value);
                return;
            }

            var floatVal = obj as FloatValue;
            if (floatVal) {
                writer.Write(floatVal.value);
                return;
            }

            var strVal = obj as StringValue;
            if (strVal)
            {
                if (strVal.isNewline)
                    writer.Write("\\n", escape:false);
                else {
                    writer.WriteStringStart();
                    writer.WriteStringInner("^");
                    writer.WriteStringInner(strVal.value);
                    writer.WriteStringEnd();
                }
                return;
            }

            var listVal = obj as ListValue;
            if (listVal)
            {
                // TODO
                //WriteInkList(writer, listVal);
                return;
            }

            var divTargetVal = obj as DivertTargetValue;
            if (divTargetVal)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("^->", divTargetVal.value.componentsString);
                writer.WriteObjectEnd();
                return;
            }

            var varPtrVal = obj as VariablePointerValue;
            if (varPtrVal)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("^var", varPtrVal.value);
                writer.WriteProperty("ci", varPtrVal.contextIndex);
                writer.WriteObjectEnd();
                return;
            }

            var glue = obj as Runtime.Glue;
            if (glue) {
                writer.Write("<>");
                return;
            }

            var controlCmd = obj as ControlCommand;
            if (controlCmd)
            {
                writer.Write(_controlCommandNames[(int)controlCmd.commandType]);
                return;
            }

            var nativeFunc = obj as Runtime.NativeFunctionCall;
            if (nativeFunc)
            {
                var name = nativeFunc.name;

                // Avoid collision with ^ used to indicate a string
                if (name == "^") name = "L^";

                writer.Write(name);
                return;
            }


            // Variable reference
            var varRef = obj as VariableReference;
            if (varRef)
            {
                writer.WriteObjectStart();

                string readCountPath = varRef.pathStringForCount;
                if (readCountPath != null)
                {
                    writer.WriteProperty("CNT?", readCountPath);
                }
                else
                {
                    writer.WriteProperty("VAR?", varRef.name);
                }

                writer.WriteObjectEnd();
                return;
            }

            // Variable assignment
            var varAss = obj as VariableAssignment;
            if (varAss)
            {
                writer.WriteObjectStart();

                string key = varAss.isGlobal ? "VAR=" : "temp=";
                writer.WriteProperty(key, varAss.variableName);

                // Reassignment?
                if (!varAss.isNewDeclaration)
                    writer.WriteProperty("re", true);

                writer.WriteObjectEnd();

                return;
            }

            // Void
            var voidObj = obj as Void;
            if (voidObj) {
                writer.Write("void");
                return;
            }

            // Tag
            var tag = obj as Tag;
            if (tag)
            {
                writer.WriteObjectStart();
                writer.WriteProperty("#", tag.text);
                Console.WriteLine( "#: " + tag.text);
                writer.WriteObjectEnd();
                return;
            }

            // Used when serialising save state only
            var choice = obj as Choice;
            if (choice) {
                WriteChoice(writer, choice);
                return;
            }

            throw new System.Exception("Failed to write runtime object to JSON: " + obj);
        }

        static Choice JObjectToChoice(Dictionary<string, object> jObj)
        {
            var choice = new Choice();
            choice.text = jObj ["text"].ToString();
            Console.Write("Choice: " + choice.text);
            choice.index = (int)jObj ["index"];
            choice.sourcePath = jObj ["originalChoicePath"].ToString();
            choice.originalThreadIndex = (int)jObj ["originalThreadIndex"];
            choice.pathStringOnChoice = jObj ["targetPath"].ToString();
            return choice;
        }

        public static void WriteChoice(SimpleChoosatron.Writer writer, Choice choice)
        {
            writer.WriteObjectStart();
            writer.WriteProperty("text", choice.text);
            writer.WriteProperty("index", choice.index);
            writer.WriteProperty("originalChoicePath", choice.sourcePath);
            writer.WriteProperty("originalThreadIndex", choice.originalThreadIndex);
            writer.WriteProperty("targetPath", choice.pathStringOnChoice);
            writer.WriteObjectEnd();
        }

        static void WriteInkList(SimpleChoosatron.Writer writer, ListValue listVal)
        {
            var rawList = listVal.value;

            writer.WriteObjectStart();

            writer.WritePropertyStart("list");

            writer.WriteObjectStart();

            foreach (var itemAndValue in rawList)
            {
                var item = itemAndValue.Key;
                int itemVal = itemAndValue.Value;

                writer.WritePropertyNameStart();
                writer.WritePropertyNameInner(item.originName ?? "?");
                writer.WritePropertyNameInner(".");
                writer.WritePropertyNameInner(item.itemName);
                writer.WritePropertyNameEnd();

                writer.Write(itemVal);

                writer.WritePropertyEnd();
            }

            writer.WriteObjectEnd();

            writer.WritePropertyEnd();

            if (rawList.Count == 0 && rawList.originNames != null && rawList.originNames.Count > 0)
            {
                writer.WritePropertyStart("origins");
                writer.WriteArrayStart();
                foreach (var name in rawList.originNames)
                    writer.Write(name);
                writer.WriteArrayEnd();
                writer.WritePropertyEnd();
            }

            writer.WriteObjectEnd();
        }

         static Choosatron() 
         {
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


        public const Byte kStartHeaderByte = 0x01;
        public const Byte kBinVerMajor = 1;
        public const Byte kBinVerMinor = 0;
        public const Byte kBinVerRev = 6;

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
}