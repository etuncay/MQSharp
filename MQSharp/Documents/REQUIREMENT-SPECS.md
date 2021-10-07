# Features
- Open source.
- Dot.Net Standard 2.0 support.
- MQTT v3.1.1 and v5.0 support
- Username, password based authentication support.
- SSL/TLS server cert and device cert authentication support.
- Easy to configure and easy to use.
- High performance with parallel processing.
- Less Memory and CPU consumption.
- Rich exception handling. And better control over exception handling.
- Built in low level trace and logging support.


# Requirements
- User will be able to set broker host and port. Host and port can be set using both constructor and property.
- If user doesn't set broker host then library will auto set the host as localhost(127.0.0.1).
- If user doesn't set the broker port then library will auto set the port. For non SSL connection the default port will be 1883 and for SSL connection default port will be 8883.
- User will be able to connect with broker as annonymouse. Library will auto generate client Id and username during connectivity with the broker.
- User will be able to connect with broker using client Id, username and password.
- User will be able to connect with broker over SSL connection.
- User will be able to connect with broker using client certificate.
- User will be able to check connection status using a property. If client connection successfull and still connection is open then this property will return true. Else this property will return false.
- Library will emit a event once connection successfull. User will be able to subscribe to that event. From the event data user will know whether connection successfull or not.
- Library will emit a event if connection closed by broker.
- User will be able to chose MQTT version between 3.1.1 and 5.0
- User will be able to chose TLS version.
- User will be able to define whether the session is cleaned or not.
- User will be able to set keep alive period.
- User will be able to set message resend interval.
- User will be able to set connection timeout time.
- User will be able to set number or auto retry if TCP connection goes down.
- User will be able to subscribe to any topic.
- Library will emit a subscribe successfull event with topci and qos level.
- User will be able to unsubscribe from any topic.
- Library will emit unsubscribe successfull event.
- User will be able to publish messsage to a set of topic with different qos level for each of the topic. Publish will return the message Id.
- Library will emit publish successfull event with message Id
- Library will emit a event when it send ping request to server on every keep alive interval.
- Library will emit a event when server return ping response for any ping request.
- Library throw immidiate erro if its faled to conect with broker.