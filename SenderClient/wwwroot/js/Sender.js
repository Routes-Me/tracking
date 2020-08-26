"use strict";

var connection = new signalR.HubConnectionBuilder().withAutomaticReconnect([1000, 10000, 30000, 60000]).withUrl("http://localhost:55202/trackServiceHub?vehicleId=456&institutionId=1234&deviceId=4567").build();

//Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("CommonMessage", function (result) {
    alert(result);
});

connection.onclose(e => {
    console.log('{ "code": "108", "message": "Server connection failed!" }')
});

//function restartConnectionIfStopped() {
//    if (connection.state == 'Disconnected') {
//        connection.start().then(function () {
//        }).catch(function (err) {
//            return console.error(err.toString());
//        });
//    }
//}
//setInterval(restartConnectionIfStopped, 10000);

document.getElementById("sendButton").addEventListener("click", function (event) {
    var Latitude = document.getElementById("Latitude").value;
    var Longitude = document.getElementById("Longitude").value;
    var timestamp = document.getElementById("TimeStamp").value;

    var model = { "SendLocation": [{ "latitude": Latitude, "longitude": Longitude, "timestamp": timestamp }] }

    connection.invoke("SendLocation", model).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});