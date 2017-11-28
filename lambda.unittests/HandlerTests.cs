using Xunit;

namespace Lambda.UnitTests
{
    public class HandlerTests
    {
        [Fact]
        public void Hello_Returns_Response_Message_And_The_Given_Request_Object()
        {
            var handler = new Handler();

            var request = new Request("Key1", "Key2", "Key3");

            var response = handler.Hello(request);

            Assert.Equal("Go Serverless v1.0! Your function executed successfully!", response.Message);

            Assert.Equal("Key1", response.Request.Key1);
            Assert.Equal("Key2", response.Request.Key2);
            Assert.Equal("Key3", response.Request.Key3);
        }
    }
}