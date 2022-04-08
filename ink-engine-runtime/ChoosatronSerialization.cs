using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace Ink.Runtime
{
    public static class Choosatron
    {
        public class Passage
        {
            public const byte kEndPsgByte = 0x03;
            public const int kAppendFlag = 7;
            public const int kContinueFlag = 6;
            byte _attributes;
            byte _ending;
            UInt32 _byteLength;
            byte[] _bytes;
            List<Operation> _updateOps = new List<Operation>();
            List<Choice> _choices = new List<Choice>();
            // string _name;
            // string _body;
            public string Name { get; set; }
            public string Body { get; set; }
            public bool IsEnding { get; set; }

            public Passage() {}

            public Passage(string aName, string aBody) {
                Name = aName;
                Body = aBody;
            }

            public UInt32 GetByteLength() { return _byteLength; }

            public byte[] GetBytes() { return _bytes; }

            public void SetAppendFlag(bool aValue) {
                _attributes = Bits.SetBit(_attributes, kAppendFlag, aValue);
            }

            public void SetContinueFlag(bool aValue) {
                _attributes = Bits.SetBit(_attributes, kContinueFlag, aValue);
            }

            public void AddChoice(Choice aChoice) {
                _choices.Add(aChoice);
            }

            public void AddUpdate(Operation aUpdate) {
                _updateOps.Add(aUpdate);
            }

            public List<Choice> GetChoices() {
                return _choices;
            }

            public Choice GetChoice(int aIndex) {
                if (aIndex >= 0 && aIndex < _choices.Count) {
                    return _choices[aIndex];
                }
                return null;
            }

            public int GetChoiceCount() { return _choices.Count; }

            public void SetEnding(string aEndTag) {
                byte.TryParse(aEndTag.Split(':')[1], out _ending);
                if (_ending < 1 || _ending > 5) {
                    _ending = 0;
                }
            }

            public UInt32 GenerateBytes() {
                ResolveAttributes();

                //MemoryStream stream = new MemoryStream();
                SimpleChoosatron.Writer writer = new SimpleChoosatron.Writer( new MemoryStream() );

                // 1 byte - Attribute flags.
                writer.Write(_attributes);

                // 1 byte - Number of update operations.
                byte updateCount = (byte)_updateOps.Count;
                writer.Write(updateCount);

                // Write update operation bytes.
                foreach (Operation op in _updateOps) {
                    writer.Write(op.ToBytes());
                }

                // 2 bytes - Lenth of the body text.
                UInt16 bodyLen = (UInt16)Body.Length;
                writer.Write(bodyLen);

                // The body content.
                byte[] data = ASCIIEncoding.ASCII.GetBytes( Body );
                writer.Write(data);

                // 1 byte - Number of choices.
                byte choiceCount = (byte)_choices.Count;
                writer.Write(choiceCount);

                if (choiceCount > 0) {
                    // Write choice bytes.
                    foreach (Choice c in _choices) {
                        writer.Write(c.ToBytes());
                    }
                } else {
                    // 1 byte - The ending quality.
                    writer.Write(_ending);
                }

                // 1 byte - End of passage byte.
                writer.Write(kEndPsgByte);

                // Save the bytes for this passage.
                _bytes = writer.Stream.ToArray();

                // Set the total length in bytes for the current state of this passage.
                _byteLength = (UInt32)_bytes.Length;

                return _byteLength;
                //return writer.Stream.ToArray();
            }

            public string ToString(string aPrefix = "") {
                ResolveAttributes();

                string output = "";
                output += "[Passage: " + Name + "]\n";
                output += aPrefix + "[Flags] " + Bits.GetBinaryString(_attributes) + "\n";
                if (_updateOps.Count > 0) {
                    output += aPrefix + "[Updates]\n";
                    foreach (Operation op in _updateOps) {
                        output += op.ToString(aPrefix);
                    }
                }
                output += aPrefix + Body + "\n";
                if (_choices.Count > 0) {
                    foreach (Choice c in _choices) {
                        output += c.ToString(aPrefix);
                    }
                } else {
                    if (_ending != 0) {
                        output += aPrefix + "[ENDING: " + _ending + "]\n";
                    } else {
                        output += aPrefix + "[ENDING]\n";
                    }
                }
                output += "[Passage Complete]\n\n";
                return output;
            }

            private void ResolveAttributes() {
                if (_choices.Count == 1) {
                    string body = _choices[0].Body;
                    if (body.Length == 0 || body.Trim().ToLower() == "<append>") {
                        _choices[0].Body = "";
                        SetAppendFlag(true);
                    } else if (body.Trim().ToLower() == "<continue>") {
                        _choices[0].Body = "";
                        SetContinueFlag(true);
                    }
                }
            }
        }

        public class Choice
        {
            byte _attributes;
            List<Operation> _conditionOps = new List<Operation>();
            List<Operation> _updateOps = new List<Operation>();
            UInt32 _byteLength;
            public UInt16 PsgIdx { get; set; }
            public string Body { get; set; }
            public string PsgLink { get; set; }
            public string StartContent { get; set; }    
            public string ChoiceOnlyContent { get; set; }    
            public string OutputContent { get; set; }    
            public bool HasCondition { get; set; }    
            public bool HasStartContent { get; set; }    
            public bool HasChoiceOnlyContent { get; set; }    
            private bool _isInvisibleDefault;
            public bool IsInvisibleDefault {
                get { return _isInvisibleDefault; }
                set {
                    _isInvisibleDefault = value;
                    _attributes = Bits.SetBit(_attributes, 0, value);
                    Console.WriteLine("Attrs: " + Bits.GetBinaryString(_attributes));
                }
            }
            private bool _onceOnly;
            public bool OnceOnly {
                get { return _onceOnly; }
                set {
                    _onceOnly = value;
                    _attributes = Bits.SetBit(_attributes, 1, value);
                    Console.WriteLine("Attrs2: " + Bits.GetBinaryString(_attributes));
                }    
            }

            public Choice() { Body = ""; }

            public void SetFlags(byte aFlags) {
                HasCondition = Bits.IsBitSetTo1(aFlags, 0);
                HasStartContent = Bits.IsBitSetTo1(aFlags, 1);
                HasChoiceOnlyContent = Bits.IsBitSetTo1(aFlags, 2);
                IsInvisibleDefault = Bits.IsBitSetTo1(aFlags, 3);
                OnceOnly = Bits.IsBitSetTo1(aFlags, 4);
                Console.WriteLine(HasCondition ? "f:condition" : "f:no condition");
                Console.WriteLine(HasStartContent ? "f:start text" : "f:no start text");
                Console.WriteLine(HasChoiceOnlyContent ? "f:choice only text" : "f:no choice only text");
                Console.WriteLine(IsInvisibleDefault ? "f:invisible" : "f:visible");
                Console.WriteLine(OnceOnly ? "f:once only" : "f:not once");

                Console.WriteLine("Flag: " + aFlags + ", Attr: " + _attributes);
            }

            public string GetChoiceText() {
                return StartContent + ChoiceOnlyContent;
            }

            public string GetOutputText() {
                return StartContent + OutputContent.Trim();
            }

            public UInt32 GetByteLength() { return _byteLength; }

            public byte[] ToBytes() {
                //MemoryStream stream = new MemoryStream();
                SimpleChoosatron.Writer writer = new SimpleChoosatron.Writer( new MemoryStream() );

                // 1 byte - Attribute flags.
                writer.Write(_attributes);

                // 1 byte - Number of conditions.
                byte condCount = (byte)_conditionOps.Count;
                writer.Write(condCount);

                // Write the data of each condition operation.
                foreach (Operation condition in _conditionOps) {
                    writer.Write(condition.ToBytes());
                }

                byte updateCount = (byte)_updateOps.Count;
                byte[][] updateBytes = new byte[updateCount][];
                UInt16 totalUpdateBytes = 0;
                for (int i = 0; i < updateCount; i++) {
                    updateBytes[i] = _updateOps[i].ToBytes();
                    totalUpdateBytes += (UInt16)updateBytes[i].Length;
                }
                // 2 bytes - Byte length of all update operation combined.
                writer.Write(totalUpdateBytes);
                // 1 byte - Number of updates.
                writer.Write(updateCount);
                // All the update operation bytes.
                foreach (byte[] opBytes in updateBytes) {
                    writer.Write(opBytes);
                }

                // 2 bytes - Length of body text.
                UInt16 bodyLen = (UInt16)Body.Length;
                writer.Write(bodyLen);

                // The body content.
                byte[] data = ASCIIEncoding.ASCII.GetBytes( Body );
                writer.Write(data);

                // 2 bytes - The index of the linked passage.
                writer.Write(PsgIdx);

                // Set the total length in bytes for the current state of this choice.
                _byteLength = (UInt32)writer.Stream.Length;

                return writer.Stream.ToArray();
            }

            public string ToString(string aPrefix = "") {
                string output = "";
                output += aPrefix + "[Choice]\n";
                output += aPrefix + aPrefix + "Flags: " + Bits.GetBinaryString(_attributes) + "\n";
                if (_conditionOps.Count > 0) {
                    output += aPrefix + aPrefix + "[Conditions]\n";
                    foreach (Operation op in _conditionOps) {
                        output += op.ToString(aPrefix + aPrefix);
                    }
                }
                if (Body.Length > 0) {
                    output += aPrefix + aPrefix + Body.Trim() + " -> " + PsgLink + ": " + PsgIdx + "\n";
                } else {
                    output += aPrefix + aPrefix + "-> " + PsgLink + "\n";
                }
                if (_updateOps.Count > 0) {
                    output += aPrefix + "[Updates]\n";
                    foreach (Operation op in _updateOps) {
                        output += op.ToString(aPrefix + aPrefix);
                    }
                }
                return output;
            }
        }

        public class Operation
        {
            // For operations, is an operand a raw value a variable, or another operation.
            public enum OperandType {
                NotSet = 0,
                Raw,
                Var,
                Op,
                Visits
            }

            public enum OperationType
            {
                NotSet = 0,
                Equal, // Returns 0 or 1
                NotEquals, // Returns 0 or 1
                Greater, // Returns 0 or 1
                Less, // Returns 0 or 1
                GreaterThanOrEquals, // Returns 0 or 1
                LessThanOrEquals, // Returns 0 or 1
                AND, // Returns 0 or 1
                OR, // Returns 0 or 1
                XOR, // Returns 0 or 1
                NAND, // Returns 0 or 1
                NOR, // Returns 0 or 1
                XNOR, // Returns 0 or 1
                ChoiceVisible, // Returns 0 or 1
                Mod, // Returns int16_t - remainder of division
                Assign, // Returns int16_t - value of the right operand
                Add, // Returns int16_t
                Subtract, // Returns int16_t
                Multiply, // Returns int16_t
                Divide, // Returns int16_t - non float, whole number
                Random, // Returns int16_t - takes min & max (inclusive)
                Dice, // Returns int16_t - take # of dice & # of sides per die
                IfStatement, // Returns 0 if false, result of right operand if true
                Negate, // Returns int16_t - positive val becomes negative, negative val becomes positive
                Not, // Returns 0 or 1 - if val not 0, returns 0, if val is 0, returns 1
                Min, // Returns int16_t - the smaller of the two numbers
                Max, // Returns int16_t - the larger of the two numbers
                // ----
                TOTAL_OPS
            }

            // OperandType _leftType;
            // // If left type is operation this will be set.
            // Operation _leftOp;
            // Int16 _leftVal;
            OperationType _opType;
            // OperandType _rightType;
            // // If right type is operation this will be set.
            // Operation _rightOp;
            // Int16 _rightVal;
            UInt32 _byteLength;

            public string LeftName { get; set; }
            public string RightName { get; set; }
            public OperandType LeftType { get; set; }
            public Operation LeftOp { get; set; }
            public Int16 LeftVal { get; set; }
            // public OperationType OpType { get; set; }
            public OperandType RightType { get; set; }
            public Operation RightOp { get; set; }
            public Int16 RightVal { get; set; }

            public Operation() { Operations(); }

            public bool IsEmpty() {
                if (LeftType == OperandType.NotSet && RightType == OperandType.NotSet) {
                    return true;
                }
                return false;
            }

            public void DeclareVariable(Int16 aVarIdx, Int16 aValue) {
                LeftType = OperandType.Var;
                LeftVal = aVarIdx;
                _opType = OperationType.Assign;
                RightType = OperandType.Raw;
                RightVal = aValue;
            }

            public bool InjectVarLeft(string aName) {
                if ((LeftType != OperandType.NotSet) && (RightType == OperandType.NotSet)) {
                    RightType = LeftType;
                    RightVal = LeftVal;
                    RightOp = LeftOp;
                    RightName = LeftName;
                    LeftType = OperandType.Var;
                    LeftVal = 0;
                    LeftName = aName;
                    _opType = OperationType.Assign;
                    return true;
                }
                return false;
            }

            public bool InjectOperationLeft(Operation aLeftOp, string aOperation) {
                if ((LeftType != OperandType.NotSet) && (RightType == OperandType.NotSet)) {
                    RightType = LeftType;
                    RightVal = LeftVal;
                    RightOp = LeftOp;
                    RightName = LeftName;
                    LeftType = OperandType.Op;
                    LeftVal = 0;
                    LeftOp = aLeftOp;
                    // Attempts to map and set the operation. False if error.
                    return SetOpType(aOperation);
                }
                return false;
            }

            public bool AddTerm(OperandType aType, string aName) {
                if (LeftType == OperandType.NotSet) {
                    LeftType = aType;
                    LeftName = aName;
                    return true;
                } else if (RightType == OperandType.NotSet) {
                    RightType = aType;
                    RightName = aName;
                    return true;
                }
                return false;
            }

            public bool AddTerm(OperandType aType, Int16 aValue) {
                if (LeftType == OperandType.NotSet) {
                    LeftType = aType;
                    LeftVal = aValue;
                    return true;
                } else if (RightType == OperandType.NotSet) {
                    RightType = aType;
                    RightVal = aValue;
                    return true;
                }
                return false;
            }

            public bool AddTerm(Operation aOp) {
                if (LeftType == OperandType.NotSet) {
                    LeftType = OperandType.Op;
                    LeftOp = aOp;
                    return true;
                } else if (RightType == OperandType.NotSet) {
                    RightType = OperandType.Op;
                    RightOp = aOp;
                    return true;
                }
                return false;
            }

            public bool SetOpType(string aOperation) {
                if (_functionMap.ContainsKey(aOperation)) {
                    _opType = _functionMap[aOperation];
                    return true;
                }
                return false;
            }

            public void SetOpType(OperationType aOp) {
                _opType = aOp;
            }

            public UInt32 GetByteLength() { return _byteLength; }

            public byte[] ToBytes() {
                //MemoryStream stream = new MemoryStream();
                SimpleChoosatron.Writer writer = new SimpleChoosatron.Writer( new MemoryStream() );

                // 1 byte for left/right types; only 4 bits each.
                byte bothTypes = (byte)LeftType;
                // Shift the left value to the left side of the byte.
                bothTypes = (byte)(bothTypes << 4);
                // Add the right side.
                bothTypes = (byte)(bothTypes & (byte)RightType);
                writer.Write(bothTypes);

                // 1 byte for operation type.
                writer.Write((byte)_opType);

                if (LeftType == OperandType.Op) {
                    writer.Write(LeftOp.ToBytes());
                } else {
                    writer.Write(LeftVal);
                }

                if (RightType == OperandType.Op) {
                    writer.Write(RightOp.ToBytes());
                } else {
                    writer.Write(RightVal);
                }

                // Set the total length in bytes for the current state of this operation.
                _byteLength = (UInt32)writer.Stream.Length;

                return writer.Stream.ToArray();
            }

            public string ToString(string aPrefix = "") {
                string output = "";

                output += aPrefix + "( ";
                if (LeftType == OperandType.Raw) {
                    output += LeftVal;
                } else if (LeftType == OperandType.Var) {
                    output += "[" + LeftName + "]";
                } else if (LeftType == OperandType.Visits) {
                    output += "p[" + LeftName + "]";
                } else if (LeftType == OperandType.Op) {
                    output += LeftOp.ToString();
                } else { // Not set yet.
                    output += "<NOT SET>";
                }

                output += " " + _operationNames[(byte)_opType] + " ";

                if (RightType == OperandType.Raw) {
                    output += RightVal;
                } else if (RightType == OperandType.Var) {
                    output += "[" + RightName + "]";
                } else if (RightType == OperandType.Visits) {
                    output += "p[" + RightName + "]";
                } else if (RightType == OperandType.Op) {
                    output += RightOp.ToString();
                } else { // Not set yet.
                    output += "<NOT SET>";
                }

                output += " )";

                return output;
            }

            static void Operations() {
                _operationNames = new string[(int)OperationType.TOTAL_OPS];

                _operationNames [(int)OperationType.NotSet] = "<n/a>";
                _operationNames [(int)OperationType.Equal] = "==";
                _operationNames [(int)OperationType.NotEquals] = "!=";
                _operationNames [(int)OperationType.Greater] = ">";
                _operationNames [(int)OperationType.Less] = "<";
                _operationNames [(int)OperationType.GreaterThanOrEquals] = ">=";
                _operationNames [(int)OperationType.LessThanOrEquals] = "<=";
                _operationNames [(int)OperationType.AND] = "AND";
                _operationNames [(int)OperationType.OR] = "OR";
                _operationNames [(int)OperationType.XOR] = "XOR";
                _operationNames [(int)OperationType.NAND] = "NAND";
                _operationNames [(int)OperationType.NOR] = "NOR";
                _operationNames [(int)OperationType.XNOR] = "XNOR";
                _operationNames [(int)OperationType.ChoiceVisible] = "ChoiceVisible";
                _operationNames [(int)OperationType.Mod] = "%";
                _operationNames [(int)OperationType.Assign] = "=";
                _operationNames [(int)OperationType.Add] = "+";
                _operationNames [(int)OperationType.Subtract] = "-";
                _operationNames [(int)OperationType.Multiply] = "*";
                _operationNames [(int)OperationType.Divide] = "/";
                _operationNames [(int)OperationType.Random] = "RANDOM";
                _operationNames [(int)OperationType.Dice] = "DICE";
                _operationNames [(int)OperationType.IfStatement] = "If";
                _operationNames [(int)OperationType.Negate] = "_";
                _operationNames [(int)OperationType.Not] = "!";
                _operationNames [(int)OperationType.Min] = "MIN";
                _operationNames [(int)OperationType.Max] = "MAX";

                for (int i = 0; i < (int)OperationType.TOTAL_OPS; ++i) {
                    if (_operationNames [i] == null) {
                        throw new System.Exception ("Operation not accounted for in serialization.");
                    }
                }
            }

            static void Functions() {
                
            }

            static string[] _operationNames;

            static Dictionary<string, OperationType> _functionMap = new Dictionary<string, OperationType> {
                { NativeFunctionCall.Equal, OperationType.Equal },
                { NativeFunctionCall.NotEquals, OperationType.NotEquals },
                { NativeFunctionCall.Greater, OperationType.Greater },
                { NativeFunctionCall.Less, OperationType.Less },
                { NativeFunctionCall.GreaterThanOrEquals, OperationType.GreaterThanOrEquals },
                { NativeFunctionCall.LessThanOrEquals, OperationType.LessThanOrEquals },
                { NativeFunctionCall.And, OperationType.AND },
                { NativeFunctionCall.Or, OperationType.OR },
                { NativeFunctionCall.Mod, OperationType.Mod },
                { NativeFunctionCall.Add, OperationType.Add },
                { NativeFunctionCall.Subtract, OperationType.Subtract },
                { NativeFunctionCall.Multiply, OperationType.Multiply },
                { NativeFunctionCall.Divide, OperationType.Divide },
                // This is a ControlCommand.
                { "RANDOM", OperationType.Random },
                // Let's try custom stuff and see what happens.
                { "DICE", OperationType.Dice },
                // I believe Ink handles 'If/Else' sort of stuff.
                { NativeFunctionCall.Negate, OperationType.Negate },
                { NativeFunctionCall.Not, OperationType.Not },
                { NativeFunctionCall.Min, OperationType.Min },
                { NativeFunctionCall.Max, OperationType.Max }
            };
        }


        public static void WriteBinary(SimpleChoosatron.Writer aWriter, Container aContainer) {
            // Parse for all passage content.            
            ParseRuntimeContainer(aContainer);

            Console.WriteLine("------------");

            // DEBUG: Print passage aliases.
            foreach (var map in _psgAliases) {
                Console.WriteLine(map.Key + ": " + map.Value);
            }

            // DEBUG: Print passages as strings.
            foreach (Passage p in _passages) {
                Console.Write(p.ToString("    "));
            }

            Console.WriteLine("Total Passages: " + _passages.Count);


            if (_varToIdx.Count >= 1) {
                Console.WriteLine("Variable Declarations");
            }
            Console.WriteLine("Var Count: " + _vars.Count);
            foreach (var map in _varToIdx) {
                Console.WriteLine(" VAR " + map.Key + " = " + _vars[map.Value]);
                Operation op = new Operation();
                op.DeclareVariable((Int16)map.Value, _vars[map.Value]);
                _passages[0].AddUpdate(op);
            }

            // Update choice links to match passage indexes.
            ResolveReferences();

            UInt32 storySize = 0;
            // Generate passage bytes.
            foreach (Passage p in _passages) {
                _psgOffsets.Add(storySize);
                storySize += p.GenerateBytes();
            }

            // Add bytes for passage count and offsets.
            storySize += 2; // Passage count bytes.
            storySize += (UInt32)(4 * _passages.Count);
            storySize += (UInt32)414; // Header size.

            // Generate and write the story header - 414 bytes
            byte[] header = BuildHeader(aWriter, aContainer.content[0] as Container, storySize, _varIdx);
            aWriter.Write( header );

            // Passage Count - 2 bytes - Location 414 / 0x019E
            aWriter.Write((UInt16)_passages.Count);

            // Passage Offsets - 4 bytes each
            foreach (UInt32 size in _psgOffsets) {
                aWriter.Write(size);
            }

            // Passage Data
            foreach (Passage p in _passages) {
                aWriter.Write(p.GetBytes());
            }
        }

        static void ResolveReferences() {
            foreach (Passage p in _passages) {
                foreach (Choice c in p.GetChoices()) {
                    if (_psgToIdx.ContainsKey(c.PsgLink)) {
                        c.PsgIdx = _psgToIdx[c.PsgLink];
                    } else {
                        c.PsgIdx = _psgToIdx[_psgAliases[c.PsgLink]];
                    }
                    // Console.WriteLine("Idx: " + c.PsgIdx);
                }
            }
        }

        static void ParseRuntimeContainer(Container aContainer, bool aWithoutName = false) {
            _indent += kIndent;           
            _dataDepth++;

            foreach (var c in aContainer.content) {
                ParseRuntimeObject(c, aContainer.path.ToString());
            }

            //Console.WriteLine("PsgDepth: " + _newPsgDepth + ", Depth: " + _dataDepth);
            
            // End of passage but the container holder additional passages.
            if ((_newPsgDepth + 1 == _dataDepth) && _state == State.Passage) {
                _state = State.None;
                if (_psg != null) {
                    Console.WriteLine("<END OF PASSAGE: " + _psg.Name);
                    _psgToIdx.Add(_psg.Name, (UInt16)_passages.Count);
                    _passages.Add(_psg);
                    _newPsgDepth = 0;
                    _psg = null;
                }
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
                Console.WriteLine("Has Terminator");
                //writer.WriteObjectStart();
            }

            if ( namedOnlyContent != null ) {
                foreach (var namedContent in namedOnlyContent) {
                    var name = namedContent.Key;
                    var namedContainer = namedContent.Value as Container;
                    Console.WriteLine(_indent + "[Named] " + name + ": " + namedContainer.path + " <State: " + _state + ">");

                    // If 'None' we are just starting to look for the beginning of a new passage.
                    if (_state == State.None) {
                        if (name == "global decl") {
                            _state = State.VarDeclarations;
                            ParseRuntimeContainer(namedContainer, aWithoutName:true);
                            _state = State.None;
                        } else {
                            // Could be a knot/stitch w/ content (like a Choosatron Passage) or
                            // could be a knot diverting to its first stitch. If first element is string, it is a passage.
                            Console.WriteLine("<START PSG: " + namedContainer.path.ToString());
                            // Depth in data structure where we are starting a new passage.
                            _newPsgDepth = _dataDepth;
                            _psg = new Passage();
                            _psg.Name = namedContainer.path.ToString();
                            _state = State.NamedContent;
                            ParseRuntimeContainer(namedContainer, aWithoutName:true);
                            // _state = State.None;
                            // if (_psg != null) {
                            //     Console.WriteLine(_indent + kIndent + "END OF PASSAGE - " + _psg.Name);
                            //     _psgToIdx.Add(_psg.Name, (UInt16)_passages.Count);
                            //     _passages.Add(_psg);
                            //     _psg = null;
                            // }
                        }
                    } else if (_state == State.Passage) {
                        if (name.StartsWith("c-")) {
                            _state = State.ChoiceOutputContent;
                            int choiceIdx = int.Parse(name.Split('-')[1]);
                            Console.WriteLine(_indent + kIndent + "<Choice Idx: " + choiceIdx);
                            _choice = _psg.GetChoice(choiceIdx);
                            ParseRuntimeContainer(namedContainer, aWithoutName:true);
                            Console.WriteLine(_indent + kIndent + "<Choice End Idx: " + choiceIdx);
                            _choice = null;
                        }
                    } else if (_state == State.ChoiceStartContent) {
                        if (name == "s") {
                            ParseRuntimeContainer(namedContainer, aWithoutName:true);
                        }
                    }
                }
            }

            if (countFlags > 0) {
                string flags = Bits.GetBinaryString((byte)countFlags);
                Console.WriteLine(_indent + "#Count Flags: " + countFlags + " '" + flags + "'");
                //writer.WriteProperty("#f", countFlags);
            }

            if (hasNameProperty) {
                Console.WriteLine(_indent + "#Name: " + aContainer.name);
                //writer.WriteProperty("#n", container.name);
            }

            if (hasTerminator) {
                //writer.WriteObjectEnd();
            } else {
                Console.WriteLine(_indent + "<null>");
                //writer.WriteNull();
            }

            _dataDepth--;
            _indent = _indent.Remove(_indent.Length - kIndent.Length);
        }

        static void ParseRuntimeObject(Runtime.Object aObj, string aParentPath) {
            var container = aObj as Container;
            if (container) {
                //_indent += kIndent;
                ParseRuntimeContainer(container);
                //_indent = _indent.Remove(_indent.Length - kIndent.Length);
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
                // This isn't a passage, but a forward to a passage.
                if (_state == State.NamedContent) {
                    Console.WriteLine(_indent + "[Divert] " + aParentPath + "->" + divert.targetPath.componentsString);
                    _state = State.None;
                    _psgAliases.Add(aParentPath, divert.targetPath.componentsString);
                    _newPsgDepth = 0;
                } else if (_state == State.GluePassage || _state == State.PassageContent) {
                    // If using a 'raw' divert, should have a glue after content.
                    // Or possibly forget '*' or '+' before divert to make it a choice.
                    if (_state == State.PassageContent) {
                        string error = "Either missing glue '<>' at the end of passage '"
                         + _psg.Name + "' content if using a divert directly after, or missing a choice command just before the divert ('*' or '+').";
                        // throw new System.Exception(error);
                        Console.WriteLine("[WARNING] " + error + " Defaulting to 'append behavior.");
                    }
                    Choice c = new Choice();
                    c.PsgLink = divert.targetPath.componentsString;
                    c.IsInvisibleDefault = true;
                    _psg.SetAppendFlag(true);
                    _psg.AddChoice(c);
                    _state = State.Passage;
                } else if (_state == State.ChoiceLink || _state == State.ChoiceOutputContent) {
                    if (!targetStr.StartsWith(".^")) {
                        Console.WriteLine(_indent + "[Divert] Choice Link->" + divert.targetPath.componentsString);
                        _choice.PsgLink = divert.targetPath.componentsString;
                        if (!_choice.IsInvisibleDefault) {
                            if (_choice.GetChoiceText() != "") {
                                _choice.Body = _choice.GetChoiceText();
                            } else {
                                _choice.Body = _choice.GetOutputText();
                            }
                            Console.WriteLine(_indent + kIndent + "Choice Text: " + _choice.Body);
                        }
                        _state = State.Passage;
                    } // There is an extra default 
                } else {
                    Console.WriteLine(_indent + "[Divert] " + divTypeKey + " " + targetStr);
                }

                return;
            }

            var choicePoint = aObj as ChoicePoint;
            if (choicePoint) {
                Console.WriteLine(_indent + "[ChoicePoint] * | " + choicePoint.pathStringOnChoice);
                byte flags = (byte)choicePoint.flags;
                Console.WriteLine(_indent + kIndent + "^ Flags: " + choicePoint.flags.ToString() + " '" + Bits.GetBinaryString(flags) + "'");

                // Must be building a passage.
                // PassageContent means no text was found yet but should have been.
                if (_state == State.Passage || _state == State.PassageContent) {
                    Console.WriteLine("<START CHOICE");
                    _choice = new Choice();
                    _choice.SetFlags(flags);
                    
                    if (_state == State.PassageContent) {
                        string error = "Choice in passage '" + _psg.Name + "' is missing any content. Either add content or remove choice command ('*' or '+') and add glue to passage content (add '<>' to end of passage text).";
                        // throw new System.Exception(error);
                        Console.WriteLine("[WARNING] " + error + " Defaulting to 'append' behavior unless there are more choices after.");
                    }
                    
                    if (_choice.HasChoiceOnlyContent) {
                        if (_choiceOnlyContent != "") {
                            _choice.ChoiceOnlyContent = _choiceOnlyContent;
                            _choiceOnlyContent = "";
                        }
                    }
                    if (_choice.HasStartContent) {
                        _state = State.ChoiceStartContent;
                        Console.WriteLine("<S:CHOICE START CONTENT");
                    } else {
                        Console.WriteLine("<S:CHOICE");
                        // Add the unfinished choice to the list and back to passage state.
                        // The next choice might have choice only text.
                        _psg.AddChoice(_choice);
                        _choice = null;
                        _state = State.Passage;
                    }
                } else {
                    throw new System.Exception("Can't parse a choice while not in a passage.");
                }
                return;
            }

            var boolVal = aObj as BoolValue;
            if (boolVal) {
                Console.WriteLine(_indent + "[Bool] " + boolVal.value);
                //writer.Write(boolVal.value);
                Int16 val = (boolVal.value ? (Int16)1 : (Int16)0);
                if (_state == State.VarDeclarations) {
                    _tempVarVal = val;
                } else if (_evaluating) {
                    if (!_opStack[_opIdx].AddTerm(Operation.OperandType.Raw, val)) {
                        throw new System.Exception ("Error adding term to operation: " + _opStack[_opIdx].ToString());
                    }
                }
                return;
            }

            var intVal = aObj as IntValue;
            if (intVal) {
                Console.WriteLine(_indent + "[Int] " + intVal.value);
                //writer.Write(intVal.value);
                if (_state == State.VarDeclarations) {
                    _tempVarVal = (Int16)intVal.value;
                } else if (_evaluating) {
                    if (!_opStack[_opIdx].AddTerm(Operation.OperandType.Raw, (Int16)intVal.value)) {
                        throw new System.Exception ("Error adding term to operation: " + _opStack[_opIdx].ToString());
                    }
                }
                return;
            }

            var floatVal = aObj as FloatValue;
            if (floatVal) {
                Console.WriteLine(_indent + "[Float] " + floatVal.value);
                //writer.Write(floatVal.value);
                throw new System.Exception("Choosatron doesn't support float values at this time.");
            }

            var strVal = aObj as StringValue;
            if (strVal) {
                if (strVal.isNewline) {
                    Console.WriteLine(_indent + "^ Newline");
                    if (_state == State.PassageContent) {
                        _psg.Body += "\n";
                    }
                    //writer.Write("\\n", escape:false);  
                } else {
                    // Is this a passage?
                    if (_state == State.NamedContent) {
                        Console.WriteLine(_indent + "[String] Psg Body: " + strVal.value);
                        // We know we are in a passage (knot/stitch).
                        _psg.Body += strVal.value;
                        _state = State.PassageContent;
                    } else if (_state == State.ChoiceStartContent) {
                        _choice.StartContent = strVal.value;
                        Console.WriteLine(_indent + "[String] Choice Start Text: " + strVal.value);

                        // Add the unfinished choice to the list and back to passage state.
                        // The next choice might have choice only text.
                        _psg.AddChoice(_choice);
                        _choice = null;
                        _state = State.Passage;
                    } else if (_state == State.Passage) {
                        Console.WriteLine(_indent + "[String] Maybe Choice Only Text: " + strVal.value);
                        _choiceOnlyContent = strVal.value;
                    } else if (_state == State.ChoiceOutputContent) {
                        Console.WriteLine(_indent + "[String] Output Text: " + strVal.value);
                        _choice.OutputContent = strVal.value;
                        _state = State.ChoiceLink;
                        Console.WriteLine(_indent + kIndent + "Choice Text: " + _choice.GetOutputText());
                    } else {
                        Console.WriteLine(_indent + "[String] UNHANDLED: " + strVal.value + " S:" + _state);
                    }
                }
                return;
            }

            var listVal = aObj as ListValue;
            if (listVal) {
                Console.WriteLine(_indent + "[ListVal]");
                //WriteInkList(writer, listVal);
                _indent += kIndent;
                
                var rawList = listVal.value;
                foreach (var itemAndValue in rawList) {
                    var item = itemAndValue.Key;
                    int itemVal = itemAndValue.Value;

                    Console.WriteLine(_indent + (item.originName ?? "?") + "." + item.itemName +": " + itemVal);
                }
                
                if (rawList.Count == 0 && rawList.originNames != null && rawList.originNames.Count > 0) {
                    Console.WriteLine(_indent + "origins:");
                    _indent += kIndent;
                    foreach (var name in rawList.originNames) {
                        Console.WriteLine(_indent + name);
                    }
                    _indent = _indent.Remove(_indent.Length - kIndent.Length);
                }

                _indent = _indent.Remove(_indent.Length - kIndent.Length);
                return;
            }

            var divTargetVal = aObj as DivertTargetValue;
            if (divTargetVal) {
                Console.WriteLine(_indent + "[DivTargetVal] ^->" + divTargetVal.value.componentsString);
                return;
            }

            var varPtrVal = aObj as VariablePointerValue;
            if (varPtrVal) {
                Console.WriteLine(_indent + "[VarPtrVal] ^var " + varPtrVal.value + ", ci " + varPtrVal.contextIndex);
                return;
            }

            var glue = aObj as Runtime.Glue;
            if (glue) {
                Console.WriteLine(_indent + "[Glue] <>");
                if (_state == State.PassageContent) {
                    _state = State.GluePassage;
                }
                //writer.Write("<>");
                return;
            }

            var controlCmd = aObj as ControlCommand;
            if (controlCmd) {
                Console.WriteLine(_indent + "[CtrlCmd] " + controlCmd.ToString());
                // Should be a Passage Update
                if (_state == State.NamedContent) {
                    if (controlCmd.commandType == ControlCommand.CommandType.EvalStart) {
                        _evaluating = true;
                        _state = State.PassageUpdate;
                        _opStack.Add(new Operation());
                        Console.WriteLine("<Start Eval>");
                    }
                } else if (_state == State.PassageUpdate) {
                    if (controlCmd.commandType == ControlCommand.CommandType.EvalEnd) {
                        // Var assignment comes after this.
                        
                    }
                } else if (_state == State.PassageContent) {
                    _state = State.Passage;
                    _psg.Body = _psg.Body.Trim();
                } else if (_state == State.Passage) {
                    if (controlCmd.commandType == ControlCommand.CommandType.End) {
                        _psg.IsEnding = true;
                    }
                }
                // + _controlCommandNames[(int)controlCmd.commandType]);
                //writer.Write(_controlCommandNames[(int)controlCmd.commandType]);
                return;
            }

            var nativeFunc = aObj as Runtime.NativeFunctionCall;
            if (nativeFunc) {
                var name = nativeFunc.name;

                // Avoid collision with ^ used to indicate a string
                if (name == "^") {
                    name = "L^";
                }
                Console.WriteLine(_indent + "[Function] " + name);
                //writer.Write(name);
                
                if (_evaluating) {
                    // // DEBUG: Output every operation on the stack.
                    // for (int i = 0; i < _opStack.Count; i++) {
                    //    Console.WriteLine("PRE-STACK: " + i + ", CURRENT OP: " + _opStack[i].ToString());
                    // }
                    // This isn't the first operation required and second operations only has left term.
                    if (_opStack[_opIdx].RightType == Operation.OperandType.NotSet) {
                        // New operation is empty, must use previous operations.
                        if (_opStack[_opIdx].LeftType == Operation.OperandType.NotSet) {
                            _opStack[_opIdx].AddTerm(_opStack[_opIdx-2]);
                            _opStack[_opIdx].AddTerm(_opStack[_opIdx-1]);
                            _opStack[_opIdx].SetOpType(name);
                            _opStack.RemoveAt(_opIdx-1);
                            _opStack.RemoveAt(_opIdx-2);
                            _opIdx--;
                            _opStack.Add(new Operation());
                        } else {
                            if (_opIdx > 0) {
                                if (!_opStack[_opIdx].InjectOperationLeft(_opStack[_opIdx-1], name)) {
                                    throw new System.Exception ("Error setting operation type: " + name);
                                }
                                _opStack.RemoveAt(_opIdx-1);
                                _opStack.Add(new Operation());
                            }
                        }
                    // Right term is set.
                    } else {
                        _opStack[_opIdx].SetOpType(name);
                        _opIdx++;
                        _opStack.Add(new Operation());
                    }
                    // DEBUG: Output every operation on the stack.
                    for (int i = 0; i < _opStack.Count; i++) {
                       Console.WriteLine("POST-STACK: " + i + ", CURRENT OP: " + _opStack[i].ToString());
                    }
                }
                return;
            }


            // Variable reference
            var varRef = aObj as VariableReference;
            if (varRef) {
                string readCountPath = varRef.pathStringForCount;
                if (readCountPath != null) {
                    Console.WriteLine(_indent + "[VarRef] CNT? " + varRef.path.ToString());
                    //writer.WriteProperty("CNT?", readCountPath);
                    if (_evaluating) {
                        if (!_opStack[_opIdx].AddTerm(Operation.OperandType.Visits, varRef.path.ToString())) {
                            throw new System.Exception ("Error adding term to operation: " + _opStack[_opIdx].ToString());
                        }
                    }
                } else {
                    Console.WriteLine(_indent + "[VarRef] VAR? " + varRef.name);
                    //writer.WriteProperty("VAR?", varRef.name);
                    if (_evaluating) {
                        if (!_opStack[_opIdx].AddTerm(Operation.OperandType.Var, varRef.name)) {
                            throw new System.Exception ("Error adding term to operation: " + _opStack[_opIdx].ToString());
                        }
                    }
                }
                return;
            }

            // Variable assignment
            var varAss = aObj as VariableAssignment;
            if (varAss) {
                string key = varAss.isGlobal ? "VAR=" : "temp=";
                //writer.WriteProperty(key, varAss.variableName);

                // Reassignment?
                if (!varAss.isNewDeclaration) {
                    //writer.WriteProperty("re", true);
                    Console.WriteLine(_indent + "[VarAss] re " + key + varAss.variableName);
                    // Either Passage or Choice update.
                    if (_evaluating) {
                        if (_state == State.PassageUpdate) {
                            // if (_opStack.Count > 2) {
                            //     throw new System.Exception ("Should only ever have up to two operations in the operation stack. It has: " + _opStack.Count);
                            // }
                            // Should be two operations on the stack. Idx 0 has the operation, idx 1 is empty for use.
                            if (_opIdx == 1) {
                                if (_opStack[_opIdx].IsEmpty()) {
                                    _opStack[_opIdx].AddTerm(Operation.OperandType.Var, varAss.variableName);
                                    _opStack[_opIdx].AddTerm(_opStack[0]);
                                    _opStack[_opIdx].SetOpType(Operation.OperationType.Assign);
                                    _opStack.RemoveAt(0);
                                    _opIdx = 0;
                                }
                            // Op index is 0. Simple assignment.
                            } else {
                                _opStack[_opIdx].InjectVarLeft(varAss.variableName);
                            }
                            Console.WriteLine("<End Eval: " + _opStack[0].ToString());
                            _psg.AddUpdate(_opStack[0]);
                            _opStack.Clear();
                            _evaluating = false;
                            _state = State.NamedContent;
                        }
                    }
                } else {
                    string varName = varAss.isGlobal ? aParentPath + " " + varAss.variableName : varAss.variableName;
                    Console.WriteLine(_indent + "[VarAss] " + key + varName);
                    if (key == "VAR=") {
                        Console.WriteLine(_indent + "[VarAss] " + key + varName);
                        _varToIdx.Add(varAss.variableName, _varIdx);
                        _varIdx++;
                        _vars.Add(_tempVarVal);
                        _tempVarVal = -999;
                    }
                }
                return;
            }

            // Void
            var voidObj = aObj as Void;
            if (voidObj) {
                Console.WriteLine(_indent + "[Void]");
                //writer.Write("void");
                return;
            }

            // Tag
            var tag = aObj as Tag;
            if (tag) {
                Console.WriteLine(_indent + "[Tag] " + tag.text);
                if (_state == State.Passage) {
                    if (tag.text.StartsWith("eq")) {
                        _psg.SetEnding(tag.text);
                    }
                }
                //writer.WriteProperty("#", tag.text);
                return;
            }

            // Used when serialising save state only
            var choice = aObj as Ink.Runtime.Choice;
            if (choice) {
                Console.WriteLine(_indent + "[Choice] " + choice);
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
                    if (parts.Length == 2) {
                        parts[1] = parts[1].Trim();
                        // Console.WriteLine( parts[0] + "-" + parts[1]);
                        tags.Add( parts[0], parts[1]);
                    }
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
            f = Bits.SetBit(f, 4, true);
            Console.WriteLine("Flag 1: " + Bits.GetBinaryString(f));
            writer.Write(f);
            f = 0;
            f = Bits.SetBit(f, 7, true);
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

        static string _indent = "";
        static int _dataDepth = 0;
        static State _state = State.None;
        static bool _evaluating = false;
        static Passage _psg;
        static Choice _choice;
        static List<Operation> _opStack = new List<Operation>();
        static UInt16 _opIdx = 0;
        static string _choiceOnlyContent;
        static UInt16 _varIdx = 0;
        static int _newPsgDepth = 0;
        static Int16 _tempVarVal = -999;
        static Dictionary<string, string> _psgAliases = new Dictionary<string, string>();
        static List<Passage> _passages = new List<Passage>();
        static Dictionary<string, UInt16> _psgToIdx = new Dictionary<string, UInt16>();
        static List<UInt32> _psgOffsets = new List<UInt32>();
        static Dictionary<string, UInt16> _varToIdx = new Dictionary<string, UInt16>();
        static List<Int16> _vars = new List<Int16>();

        public const string kIndent = "  ";

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

        enum State {
            None,
            NamedContent,
            PassageUpdate,
            PassageContent,
            Passage,
            GluePassage,
            Choice,
            ChoiceCondition,
            ChoiceStartContent,
            ChoiceOnlyContent,
            ChoiceOutputContent,
            ChoiceUpdate,
            ChoiceLink,
            VarDeclarations
        };
    }

    static class Bits
    {
        public static byte SetBitTo1(this int value, int position) {
            // Set a bit at position to 1.
            return (byte)(value |= (1 << position));
        }

        public static byte SetBitTo0(this int value, int position) {
            // Set a bit at position to 0.
            return (byte)(value & ~(1 << position));
        }

        // Position is index from right to left (0 is far right position).
        public static byte SetBit(this int value, int position, bool state) {
            if (state) {
                // Set a bit at position to 1.
                return (byte)(value |= (1 << position));   
            }
            // Set a bit at position to 0.
            return (byte)(value & ~(1 << position));
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