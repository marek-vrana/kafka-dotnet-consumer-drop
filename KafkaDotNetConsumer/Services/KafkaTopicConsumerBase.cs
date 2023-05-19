using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KafkaDotNetConsumer.Services
{
    public class KafkaTopicConsumer1 : KafkaTopicConsumerBase
    {
        public KafkaTopicConsumer1(KafkaExtendedSettings config, 
            ILogger<KafkaTopicConsumerBase> logger, 
            IHostApplicationLifetime applicationLifetime) : 
            base(config, logger, applicationLifetime, "topic1")
        {
        }
    }
    
    public class KafkaTopicConsumer2 : KafkaTopicConsumerBase
    {
        public KafkaTopicConsumer2(KafkaExtendedSettings config, 
            ILogger<KafkaTopicConsumerBase> logger, 
            IHostApplicationLifetime applicationLifetime) : 
            base(config, logger, applicationLifetime, "topic2")
        {
        }
    }
    
    public class KafkaTopicConsumer3 : KafkaTopicConsumerBase
    {
        public KafkaTopicConsumer3(KafkaExtendedSettings config, 
            ILogger<KafkaTopicConsumerBase> logger, 
            IHostApplicationLifetime applicationLifetime) : 
            base(config, logger, applicationLifetime, "topic3")
        {
        }
    }

    public class KafkaTopicConsumerBase : BackgroundService
    {
        private readonly ConsumerConfig _consumerConfig;
        private readonly KafkaExtendedSettings _config;
        private readonly ILogger<KafkaTopicConsumerBase> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly string _topicToSubscribe;
        private IConsumer<string, string>? _consumer;

        public KafkaTopicConsumerBase(
            KafkaExtendedSettings config,
            ILogger<KafkaTopicConsumerBase> logger,
            IHostApplicationLifetime applicationLifetime, 
            string topicToSubscribe)
        {
            _consumerConfig = config.ConsumerConfig;
            _config = config;
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _topicToSubscribe = topicToSubscribe;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Starting background job consumer for '{_topicToSubscribe}'");

            var consumerBuilder = new ConsumerBuilder<string, string>(_consumerConfig);

            if (_config.ShouldSetLogHandler)
            {
                _logger.LogInformation("Setting log handler");
                consumerBuilder.SetLogHandler((consumer, message) =>
                {
                    _logger.LogInformation(
                        "Kafka log for member {memberId} with subscriptions \"{subscriptions}\".\nMessage: {message}",
                        consumer.MemberId,
                        string.Join(",", consumer.Subscription),
                        message.Message);
                });
            }

            _consumer = consumerBuilder.Build();

            return Task.Run(async () => await Poll(_consumer, _topicToSubscribe), stoppingToken);
        }

        private Task Poll(IConsumer<string, string> consumer, string topicToSubscribe)
        {
            WaitUntilAppIsFullyStarted();

            consumer.Subscribe(topicToSubscribe);

            while (true)
            {
                try
                {
                    var consumeResult = consumer.Consume();

                    _logger.LogInformation($"Received message: {consumeResult.Message.Key}-{consumeResult.Message.Value}");

                    consumer.StoreOffset(consumeResult);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void WaitUntilAppIsFullyStarted()
        {
            bool stoppingBeforeFullyStarted = _applicationLifetime.ApplicationStopping.IsCancellationRequested;
            while (!_applicationLifetime.ApplicationStarted.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(100)) && !stoppingBeforeFullyStarted)
            {
                stoppingBeforeFullyStarted = _applicationLifetime.ApplicationStopping.IsCancellationRequested;
            }
        }

        public override void Dispose()
        {
            _consumer?.Close();
            _consumer?.Dispose();

            base.Dispose();
        }
    }
}