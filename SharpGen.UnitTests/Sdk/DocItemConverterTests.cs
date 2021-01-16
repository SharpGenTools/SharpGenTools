using System.Linq;
using System.Text.Json;
using SharpGenTools.Sdk.Documentation;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Sdk
{
    public class DocItemConverterTests : TestBase
    {
        public DocItemConverterTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void Roundtrip()
        {
            var item = new DocItem
            {
                Names = { "Test1" },
                Summary = "Test2",
                Items =
                {
                    new DocSubItem
                    {
                        Term = "Test3",
                        Description = "Test4"
                    }
                },
                ShortId = "Test5"
            };

            Assert.True(item.IsDirty);

            var s = JsonSerializer.Serialize(item, DocItemCache.JsonSerializerOptions);

            Assert.False(item.IsDirty);

            Assert.Equal(@"{""ShortId"":""Test5"",""Names"":[""Test1""],""Summary"":""Test2"",""Remarks"":null,""Return"":null,""Items"":[{""Term"":""Test3"",""Description"":""Test4"",""Attributes"":[]}],""SeeAlso"":[]}", s);

            var jsonItem = JsonSerializer.Deserialize<DocItem>(s, DocItemCache.JsonSerializerOptions);

            Assert.NotNull(jsonItem);
            Assert.False(jsonItem.IsDirty);
            Assert.Equal(item.ShortId, jsonItem.ShortId);
            Assert.Equal(item.Names.Count, jsonItem.Names.Count);
            Assert.Equal(item.Summary, jsonItem.Summary);
            Assert.Equal(item.Remarks, jsonItem.Remarks);
            Assert.Equal(item.Return, jsonItem.Return);
            Assert.Equal(item.Items.Count, jsonItem.Items.Count);
            Assert.Equal(item.SeeAlso.Count, jsonItem.SeeAlso.Count);
            Assert.Collection(
                jsonItem.Names,
                x => Assert.Equal(item.Names.First(), x)
            );
            Assert.Collection(
                jsonItem.Items,
                x =>
                {
                    Assert.Equal(item.Items.First().Term, x.Term);
                    Assert.Equal(item.Items.First().Description, x.Description);
                }
            );
        }
    }
}