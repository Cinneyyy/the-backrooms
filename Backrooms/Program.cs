using Backrooms.Serialization;

namespace Backrooms;

internal class Program
{
    public class Test : Serializable<Test>
    {
        public class SubTest : Serializable<SubTest>
        {
            public bool isGay;
            public string youtubeChannel;

            public override string ToString() => $"[isGay: {isGay}, youtubeChannel: \"{youtubeChannel}\"]";
        }

        public int number;
        public Arr<float> constants;
        public string joeBidensName;
        public Arr<SubTest> subTests;

        public override string ToString() => $"[number: {number}, constants: {constants}, joeBidensName: \"{joeBidensName}\", subTests: {subTests}]";
    }

    private static void Main(string[] args)
    {
        Test test = new() {
            number = 69,
            constants = [420.69f, 2.7182818284f, 3.14159f],
            joeBidensName = "Joesef Johanna Bidon",
            subTests = [
                new Test.SubTest() { isGay = false, youtubeChannel = "pornhub.de" },
                new Test.SubTest() { isGay = true, youtubeChannel = "mrbeast" }
            ]
        };

        Out(test);

        byte[] serialized = BinarySerializer<Test>.Serialize(test, System.IO.Compression.CompressionLevel.NoCompression);
        Out(serialized.FormatStr(" "));

        Test restored = BinarySerializer<Test>.Deserialize(serialized, false);
        Out(restored);

        return;
        Window window = new(new(1920/6, 1080/6), "The Backrooms", "oli_appicon", false, false, w => _ = new Game(w));
    }
}