using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;
using System.Net.Http;
using System.IO;

using FileUploadRestful.UploadServer;
using System.Linq;

namespace Server.AspNetCore.Test
{
    [TestClass]
    public class UploadTestRestful
    {

        const string BaseUrlServer = "http://localhost:54276/Upload/";

        const int ChunkSize = 0x10000;

        [TestMethod]
        public async Task TestMethod1()
        {

            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrlServer);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));



            var response =  await client.PostAsync("NewUploadQueue", new ByteArrayContent(new byte[] { }));
            if(response.IsSuccessStatusCode)
            {
                var QueueId = await response.Content.ReadAsStringAsync();

                var fl = File.OpenRead(@"..\..\..\..\FileUploadRestful\Server.Test\Testdata\CenterMilkyWay.jpg");                

                var buffer = new byte[ChunkSize];
                var chunkNo = 0L;
                bool uploadFailed = false;

                while (fl.Position < fl.Length && !uploadFailed)
                {
                    var count = await fl.ReadAsync(buffer, 0, ChunkSize);
                    if (count > 0)
                    {
                        chunkNo++;

                        //var msg =
                        //new{
                        //    QueueId = QueueId,
                        //    ChunkNo = chunkNo,
                        //    Chnunk = buffer
                        //};


                        //var settings = new Newtonsoft.Json.JsonSerializerSettings();
                        
                        //var json = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                        //var mem = new MemoryStream();
                        //var stream = new StreamWriter(mem);
                        //stream.Write(json);
                        //var barr = new ByteArrayContent(mem.ToArray());
                        //barr.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                        var barr = new ByteArrayContent(buffer);


                        //response =  await client.PostAsync($"UploadChunk?QueueId={QueueId}&ChunkNo={chunkNo}&CountBytes={count}", barr);                        
                        response = await client.PostAsync($"UploadChunk?QueueId={QueueId}&ChunkNo={chunkNo}&CountBytes={count}", barr);
                        uploadFailed = !response.IsSuccessStatusCode;                        
                    }
                }

                if (!uploadFailed)
                {
                    response = await client.GetAsync($"Statistics");
                    if (response.IsSuccessStatusCode)
                    {
                        var rc = await response.Content.ReadAsStringAsync();
                        var stat = Newtonsoft.Json.JsonConvert.DeserializeObject<mko.Logging.RCBuilder<UploadServerStatisticsBuilder>>(rc).CreateRC();

                        Assert.IsTrue(stat.Succeeded);
                        //Assert.AreEqual(1, stat.Value.NumberOfCurrentlyActiveQueues);
                        //Assert.AreEqual(stat.Value.MinLengthsOfActiveQueues, stat.Value.MaxLengthsOfActiveQueues);

                        var filename = @"C:\Users\marti_000\Documents\prj\FileUploadRestful\Server.Test\Testdata\CopyOfMilkiWayCenter.jpg";
                        if (File.Exists(filename))
                        {
                            // Delete results from previously tests.
                            File.Delete(filename);
                        }

                        if (chunkNo > 0)
                        {
                            response=  await client.PutAsync($"AppendingToFile?QueueId={QueueId}&maxChunkNo={chunkNo}", new StringContent(filename));
                            if (response.IsSuccessStatusCode)
                            {
                                var rcStr = await response.Content.ReadAsStringAsync();
                                var rcAppend = Newtonsoft.Json.JsonConvert.DeserializeObject<mko.Logging.RCBuilder<AppendingToFileLogBuilder>>(rcStr).CreateRC();
                                while (rcAppend.Succeeded)
                                {
                                    response = await client.PutAsync($"AppendingToFile?QueueId={QueueId}&maxChunkNo={chunkNo}", new StringContent(filename));
                                    if (response.IsSuccessStatusCode)
                                    {
                                        rcStr = await response.Content.ReadAsStringAsync();
                                        rcAppend = Newtonsoft.Json.JsonConvert.DeserializeObject<mko.Logging.RCBuilder<AppendingToFileLogBuilder>>(rcStr).CreateRC();
                                        Assert.IsTrue(rcAppend.Value.ErrorType == AppendingToFileErrorTypes.none, $"ErrorType{rcAppend.Value.ErrorType}: {rcAppend.Value.Description}");
                                        //Assert.AreEqual(chunkNo, rcAppend.Value.NoOfRecentlyAppendedChunk);
                                    }
                                    else
                                    {
                                        Assert.Fail("Append failed");
                                    }
                                }

                                //DeleteUploadQueue
                                response = await client.DeleteAsync($"DeleteUploadQueue?QueueId={QueueId}");
                                Assert.IsTrue(response.IsSuccessStatusCode);
                            } else
                            {
                                Assert.Fail("Appending");
                            }
                        }
                        else
                        {
                            response = await client.PostAsync($"DeleteUploadQueue?QueueId={QueueId}", new ByteArrayContent(new byte[] { }));
                            Assert.IsTrue(response.IsSuccessStatusCode);
                        }
                    } else
                    {
                        Assert.Fail("GetStatisatics");
                    }
                }else
                {
                    Assert.Fail("UploadFailed");
                }
            }
            else
            {
                Assert.Fail($"NewUploadQueue failed with HTTP Status:{response.StatusCode}");
            }


        }
    }
}
