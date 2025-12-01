using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System;
namespace Calculator.Services
{
    public class KafkaProducerHandler : IDisposable
    {
        IProducer<byte[], byte[]> kafkaProducer;
        public KafkaProducerHandler(IConfiguration config)
    
        {
            var conf = new ProducerConfig();
            config.GetSection("Kafka:ProducerSettings").
            Bind(conf);
            kafkaProducer = new ProducerBuilder<byte[],byte[]>(conf).Build();
        }
        public Handle Handle { get => kafkaProducer.Handle; }
        public void Dispose()
        {
        // Block until all outstanding produce requests have completed (with or
        // without error).
        kafkaProducer.Flush();
        kafkaProducer.Dispose();
        }
    }
}