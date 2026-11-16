using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// GM2（General MIDI Level 2）の音色名とドラムセット名。
    /// 音源定義ファイルで名前が見つからないときに使い、同梱の音源定義ファイル（GM2.xml）の元にもする。
    /// </summary>
    /// <remarks>
    /// 楽器の音色はバンク MSB 121 で、LSB 0 が GM1 と同じ 128 音色、LSB 1 以降がそのバリエーション（全 256 音色）。
    /// LSB 0 の名前は、なじみのある GM1 の名前にしている。
    /// ドラムセットはバンク MSB 120（PC の番号は GS と同じ）。
    /// </remarks>
    public static class Gm2Sounds
    {
        public const int MelodyBankMsb = 121;
        public const int DrumBankMsb = 120;

        /// <summary>プログラム番号（0～127）ごとの、LSB 0, 1, 2… の音色名。</summary>
        private static readonly string[][] Instruments =
        {
            new[] { "Acoustic Grand Piano", "Wide Acoustic Grand Piano", "Dark Acoustic Grand Piano" },
            new[] { "Bright Acoustic Piano", "Wide Bright Acoustic Piano" },
            new[] { "Electric Grand Piano", "Wide Electric Grand Piano" },
            new[] { "Honky-tonk Piano", "Wide Honky-tonk Piano" },
            new[] { "Electric Piano 1", "Detuned Electric Piano 1", "Variation Electric Piano 1", "60's Electric Piano" },
            new[] { "Electric Piano 2", "Detuned Electric Piano 2", "Variation Electric Piano 2", "Electric Piano Legend", "Phaser Electric Piano" },
            new[] { "Harpsichord", "Coupled Harpsichord", "Wide Harpsichord", "Open Harpsichord" },
            new[] { "Clavi", "Pulsed Clavinet" },
            new[] { "Celesta" },
            new[] { "Glockenspiel" },
            new[] { "Music Box" },
            new[] { "Vibraphone", "Wet Vibraphone" },
            new[] { "Marimba", "Wide Marimba" },
            new[] { "Xylophone" },
            new[] { "Tubular Bells", "Church Bells", "Carillon Bells" },
            new[] { "Dulcimer" },
            new[] { "Drawbar Organ", "Detuned Drawbar Organ", "60's Drawbar Organ", "Drawbar Organ 2" },
            new[] { "Percussive Organ", "Detuned Percussive B3 Organ", "Percussive B3 Organ 2" },
            new[] { "Rock Organ" },
            new[] { "Church Organ", "Church Organ 2", "Church Organ 3" },
            new[] { "Reed Organ", "Puffs Organ" },
            new[] { "Accordion", "Italian Accordion" },
            new[] { "Harmonica" },
            new[] { "Tango Accordion" },
            new[] { "Acoustic Guitar (nylon)", "Ukulele", "Opened Nylon-Strings Guitar", "Nylon-Strings Guitar 2" },
            new[] { "Acoustic Guitar (steel)", "12-Strings Steel Guitar", "Mandolin", "Steel-Strings Guitar + Body Tapped Sounds" },
            new[] { "Electric Guitar (jazz)", "Hawaiian Guitar" },
            new[] { "Electric Guitar (clean)", "Chorus Guitar", "Mid Tone Guitar" },
            new[] { "Electric Guitar (muted)", "Funky Guitar 1", "Funky Guitar 2", "Jazz Man" },
            new[] { "Overdriven Guitar", "Guitar Pinch" },
            new[] { "Distortion Guitar", "Feedback Guitar", "Distortion Rhythm Guitar" },
            new[] { "Guitar Harmonics", "Guitar Feedback" },
            new[] { "Acoustic Bass" },
            new[] { "Electric Bass (finger)", "Fingered Slap Bass" },
            new[] { "Electric Bass (pick)" },
            new[] { "Fretless Bass" },
            new[] { "Slap Bass 1" },
            new[] { "Slap Bass 2" },
            new[] { "Synth Bass 1", "Synth Bass 101", "Synth Bass 3", "Clavi Bass", "Hammered Bass" },
            new[] { "Synth Bass 2", "Synth Bass 4", "Rubber Bass", "Attack Pulsed" },
            new[] { "Violin", "Slow Violin" },
            new[] { "Viola" },
            new[] { "Cello" },
            new[] { "Contrabass" },
            new[] { "Tremolo Strings" },
            new[] { "Pizzicato Strings" },
            new[] { "Orchestral Harp", "Yangqin" },
            new[] { "Timpani" },
            new[] { "String Ensemble 1", "Orchestral Strings", "60's Strings" },
            new[] { "String Ensemble 2" },
            new[] { "Synth Strings 1", "Synth Strings 3" },
            new[] { "Synth Strings 2" },
            new[] { "Choir Aahs", "Choir Aahs 2" },
            new[] { "Voice Oohs", "Humming" },
            new[] { "Synth Voice", "Analog Voice" },
            new[] { "Orchestra Hit", "Bass Hit", "6th Hit", "Euro Hit" },
            new[] { "Trumpet", "Dark Trumpet Soft" },
            new[] { "Trombone", "Trombone 2", "Bright Trombone" },
            new[] { "Tuba" },
            new[] { "Muted Trumpet", "Muted Trumpet 2" },
            new[] { "French Horn", "French Horns 2" },
            new[] { "Brass Section", "Brass Section 2" },
            new[] { "Synth Brass 1", "Synth Brass 3", "Analog Brass 1", "Jump Brass" },
            new[] { "Synth Brass 2", "Synth Brass 4", "Analog Brass 2" },
            new[] { "Soprano Sax" },
            new[] { "Alto Sax" },
            new[] { "Tenor Sax" },
            new[] { "Baritone Sax" },
            new[] { "Oboe" },
            new[] { "English Horn" },
            new[] { "Bassoon" },
            new[] { "Clarinet" },
            new[] { "Piccolo" },
            new[] { "Flute" },
            new[] { "Recorder" },
            new[] { "Pan Flute" },
            new[] { "Blown Bottle" },
            new[] { "Shakuhachi" },
            new[] { "Whistle" },
            new[] { "Ocarina" },
            new[] { "Lead 1 (square)", "Square Wave", "Sine Wave" },
            new[] { "Lead 2 (sawtooth)", "Saw Wave", "Doctor Solo", "Natural Lead", "Sequenced Saw" },
            new[] { "Lead 3 (calliope)" },
            new[] { "Lead 4 (chiff)" },
            new[] { "Lead 5 (charang)", "Wire Lead" },
            new[] { "Lead 6 (voice)" },
            new[] { "Lead 7 (fifths)" },
            new[] { "Lead 8 (bass + lead)", "Delayed Lead" },
            new[] { "Pad 1 (new age)" },
            new[] { "Pad 2 (warm)", "Sine Pad" },
            new[] { "Pad 3 (polysynth)" },
            new[] { "Pad 4 (choir)", "Itopia" },
            new[] { "Pad 5 (bowed)" },
            new[] { "Pad 6 (metallic)" },
            new[] { "Pad 7 (halo)" },
            new[] { "Pad 8 (sweep)" },
            new[] { "FX 1 (rain)" },
            new[] { "FX 2 (soundtrack)" },
            new[] { "FX 3 (crystal)", "Synth Mallet" },
            new[] { "FX 4 (atmosphere)" },
            new[] { "FX 5 (brightness)" },
            new[] { "FX 6 (goblins)" },
            new[] { "FX 7 (echoes)", "Echo Bell", "Echo Pan" },
            new[] { "FX 8 (sci-fi)" },
            new[] { "Sitar", "Sitar 2" },
            new[] { "Banjo" },
            new[] { "Shamisen" },
            new[] { "Koto", "Taisho Koto" },
            new[] { "Kalimba" },
            new[] { "Bagpipe" },
            new[] { "Fiddle" },
            new[] { "Shanai" },
            new[] { "Tinkle Bell" },
            new[] { "Agogo" },
            new[] { "Steel Drums" },
            new[] { "Woodblock", "Castanets" },
            new[] { "Taiko Drum", "Concert Bass Drum" },
            new[] { "Melodic Tom", "Melodic Tom 2" },
            new[] { "Synth Drum", "808 Toms", "Electric Percussion" },
            new[] { "Reverse Cymbal" },
            new[] { "Guitar Fret Noise", "Guitar Cut Noise", "String Slap" },
            new[] { "Breath Noise", "Flute Key Click" },
            new[] { "Seashore", "Rain", "Thunder", "Wind", "Stream", "Bubble" },
            new[] { "Bird Tweet", "Dog", "Horse Gallop", "Bird Tweet 2" },
            new[] { "Telephone Ring", "Telephone Ring 2", "Door Creaking", "Door Closing", "Scratch", "Wind Chimes" },
            new[] { "Helicopter", "Car Engine", "Car Stop", "Car Pass", "Car Crash", "Siren", "Train", "Jet Plane", "Starship", "Burst Noise" },
            new[] { "Applause", "Laughter", "Screaming", "Punch", "Heartbeat", "Footsteps" },
            new[] { "Gunshot", "Machine Gun", "Laser Gun", "Explosion" },
        };

        /// <summary>ドラムセット（プログラム番号 0～127 と名前）。</summary>
        private static readonly KeyValuePair<int, string>[] DrumSets =
        {
            new KeyValuePair<int, string>(0, "Standard Kit"),
            new KeyValuePair<int, string>(8, "Room Kit"),
            new KeyValuePair<int, string>(16, "Power Kit"),
            new KeyValuePair<int, string>(24, "Electronic Kit"),
            new KeyValuePair<int, string>(25, "Analog Kit"),
            new KeyValuePair<int, string>(32, "Jazz Kit"),
            new KeyValuePair<int, string>(40, "Brush Kit"),
            new KeyValuePair<int, string>(48, "Orchestra Kit"),
            new KeyValuePair<int, string>(56, "SFX Kit"),
        };

        /// <summary>スタンダードキットの、キー 27～87 の楽器名（GM2 の打楽器の配置）。</summary>
        private const int FirstDrumKey = 27;

        private static readonly string[] DrumKeys =
        {
            "High Q", "Slap", "Scratch Push", "Scratch Pull", "Sticks", "Square Click", "Metronome Click", "Metronome Bell",
            "Acoustic Bass Drum", "Bass Drum 1", "Side Stick", "Acoustic Snare", "Hand Clap", "Electric Snare",
            "Low Floor Tom", "Closed Hi Hat", "High Floor Tom", "Pedal Hi-Hat", "Low Tom", "Open Hi-Hat",
            "Low-Mid Tom", "Hi-Mid Tom", "Crash Cymbal 1", "High Tom", "Ride Cymbal 1", "Chinese Cymbal",
            "Ride Bell", "Tambourine", "Splash Cymbal", "Cowbell", "Crash Cymbal 2", "Vibraslap",
            "Ride Cymbal 2", "Hi Bongo", "Low Bongo", "Mute Hi Conga", "Open Hi Conga", "Low Conga",
            "High Timbale", "Low Timbale", "High Agogo", "Low Agogo", "Cabasa", "Maracas",
            "Short Whistle", "Long Whistle", "Short Guiro", "Long Guiro", "Claves", "Hi Wood Block",
            "Low Wood Block", "Mute Cuica", "Open Cuica", "Mute Triangle", "Open Triangle",
            "Shaker", "Jingle Bell", "Belltree", "Castanets", "Mute Surdo", "Open Surdo",
        };

        /// <summary>
        /// GM2 の名前。楽器はバンク MSB 121 の LSB が合う音色、無ければ LSB 0 の音色。
        /// ドラムは、プログラム番号が合うドラムセット、無ければそれより前で一番近いもの（GS の音源と同じ扱い）。
        /// </summary>
        public static string GetName(InstrumentState state)
        {
            if (state.IsDrum)
            {
                string name = DrumSets[0].Value;
                foreach (KeyValuePair<int, string> set in DrumSets)
                {
                    if (set.Key <= state.Program)
                    {
                        name = set.Value;
                    }
                }
                return name;
            }

            string[] variations = Instruments[state.Program];
            bool gm2Bank = state.BankMsb == MelodyBankMsb || state.BankMsb < 0;
            return gm2Bank && state.BankLsb > 0 && state.BankLsb < variations.Length ? variations[state.BankLsb] : variations[0];
        }

        /// <summary>
        /// GM2 の音源定義ファイル（Domino の形式。文字コードは Domino に合わせて Shift_JIS）を書き出す。
        /// </summary>
        public static void WriteDefinitionFile(string filePath)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.GetEncoding(932),
                Indent = true,
                IndentChars = "\t",
            };
            string temporaryPath = filePath + ".tmp";
            using (XmlWriter writer = XmlWriter.Create(temporaryPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteComment(
                    " GM2 (General MIDI Level 2) の音源定義ファイル（Domino の形式）。KMIDI Player が作りました。自由に書き換えられます。"
                    + " このファイルを消すと、次に KMIDI Player を起動したときに作り直します。"
                    + " / GM2 instrument definition in the Domino format, created by KMIDI Player. You can edit it freely."
                    + " If you delete this file, KMIDI Player recreates it on the next start. ");
                writer.WriteStartElement("ModuleData");
                writer.WriteAttributeString("Name", "GM2");
                writer.WriteAttributeString("Folder", "");
                writer.WriteAttributeString("Priority", "2");
                writer.WriteAttributeString("FileCreator", "KMIDI Player");
                writer.WriteAttributeString("FileVersion", "1.00");

                writer.WriteStartElement("InstrumentList");
                writer.WriteStartElement("Map");
                writer.WriteAttributeString("Name", "GM2");
                for (int program = 0; program < Instruments.Length; program++)
                {
                    string[] variations = Instruments[program];
                    WriteProgram(writer, program, variations[0]);
                    for (int lsb = 0; lsb < variations.Length; lsb++)
                    {
                        WriteBank(writer, variations[lsb], MelodyBankMsb, lsb);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("DrumSetList");
                writer.WriteStartElement("Map");
                writer.WriteAttributeString("Name", "GM2");
                foreach (KeyValuePair<int, string> set in DrumSets)
                {
                    WriteProgram(writer, set.Key, set.Value);
                    WriteBank(writer, set.Value, DrumBankMsb, 0);
                    if (set.Key == 0)
                    {
                        // キーごとの楽器名は、どのセットでも同じとは限らないので、スタンダードキットだけに書く
                        for (int i = 0; i < DrumKeys.Length; i++)
                        {
                            writer.WriteStartElement("Tone");
                            writer.WriteAttributeString("Name", DrumKeys[i]);
                            writer.WriteAttributeString("Key", (FirstDrumKey + i).ToString());
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            // 書きかけのファイルを残さないよう、書き終えてから名前を変える
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.Move(temporaryPath, filePath);
        }

        private static void WriteProgram(XmlWriter writer, int program, string name)
        {
            writer.WriteStartElement("PC");
            writer.WriteAttributeString("Name", name);
            writer.WriteAttributeString("PC", (program + 1).ToString());
        }

        /// <summary>Bank の開始タグを書く（閉じるのは呼び出し側）。</summary>
        private static void WriteBank(XmlWriter writer, string name, int msb, int lsb)
        {
            writer.WriteStartElement("Bank");
            writer.WriteAttributeString("Name", name);
            writer.WriteAttributeString("MSB", msb.ToString());
            writer.WriteAttributeString("LSB", lsb.ToString());
        }
    }
}
