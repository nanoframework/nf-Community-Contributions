# nanoFramework Web Server
ESP32 Web Server for nanoFramework

This is a simple but powerful web server that allows the user to create a web server and application on one ESP board. In the past most applications used two chips one for the application and one to connect to the network. 

The server request is exposed to the application using the ServerRequest event. Two public properties are used to process the server request. First is the clientSocket that is used to return the server response to the requesting URL. The second property is the RequestString that is the string sent by the requesting URL. 

The web server runs on it's own thread so it is not blocked by the request processing. The request could be processed to control leds, motors, solenoid etc. Files of any type could be sent to the requesting URL.

The server is used by typing in the web server's assigned IP address into any web browser.
