namespace Acetylene.Tests {
    public class IgnitorTests {
        [Fact]
        public void IgnitorParsingTest() {
            // Arrange
            var ignitionFile =
                     /*lang=json,strict*/
                     @"{
  ""ignition"": { ""version"": ""3.3.0"" },
  ""passwd"": {
    ""users"": [
        {
        ""name"": ""root"",
        ""passwordHash"": ""pemFK1OejzrTI""
        }
    ]
  }
}
";
            var ignitor = new Ignitor();

            // Act
            var result = ignitor.Parse(ignitionFile);

            // Assert
            result.Ignition.Version.Should().Be("3.3.0");
            result.Passwd.Users.Should().HaveCount(1);
        }
    }
}