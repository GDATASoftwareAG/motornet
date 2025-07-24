This example shows how to use MotorNET to publish to multiple different queues.
The method shown here uses a RabbitMQ but of course the pattern can be applied to any of the
other publishers as well. Or it can be combined with other services as well.
So suppose you have a SingleOutputService which always publishes to a certain queue,
but in some cases you might want to send data to another queue.
