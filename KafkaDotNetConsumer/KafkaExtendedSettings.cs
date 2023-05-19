using Confluent.Kafka;

namespace KafkaDotNetConsumer
{
    public class KafkaExtendedSettings
    {
        public ConsumerConfig ConsumerConfig { get; set; } = null!;
        public bool ShouldSetLogHandler { get; set; }
    }
}
