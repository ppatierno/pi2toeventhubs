# Pi2 To Event Hubs

**Telemtry from Windows 10 IoT Core on Raspberry Pi 2 to Azure (Event Hubs)**

Source code demo for the session "Telemetry from Windows 10 IoT Core on Raspberry Pi 2 to Event Hubs" at Mobile Camp 2015 in Naples. The source code shows how to acquire temperature data from a TMP102 sensor (I2C based) connected to the Raspberry Pi 2 (with Windows 10 IoT Core) and send it to the Azure Event Hubs displaying it within the web site app from ConnectTheDots project.

**Projects**

The solution is for Visual Studio 2015 (RC) under Windows 10 (build release). 
Projects inside the pi2toeventhubs solution :

* **IoTClient** : class that contains the logic to acquire data and sends them to the Azure Event Hubs. It contains a base client who sends data in AMQP properties and another one for [ConnectTheDots](https://github.com/MSOpenTech/connectthedots) client (it's like tha base client but sends information in JSON format useful to the ConnectTheDots project).
* **IoTCoreSensors** : project contains the driver for TMP102 temperature sensor.
* **Pi2ToEventHubs** : UWP (Universal Windows Platform) application with a UI for sending data (temperature value) from Pi2 to Event Hubs.
* **Pi2ToEventHubsBackTask** : Background Task application (UWP app) without UI for sending data (temperature value) from Pi2 to Event Hubs.
* **Pi2EventHubProcessor** : simple console application that used an Event Hub Processor to acquire data from Event Hubs (data sent by the Pi2).

Other projects needed for this solution :

* [Azure SB Lite](http://azuresblite.codeplex.com/) : library for connecting to the Azure Service Bus services (Queues, Topics/Subscriptions and Event Hubs) using AMQP protocol. It's based on [AMQP .Net Lite library](http://amqpnetlite.codeplex.com/)
