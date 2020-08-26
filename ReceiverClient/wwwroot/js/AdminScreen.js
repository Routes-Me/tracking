var connection = new signalR.HubConnectionBuilder().withUrl("http://localhost:55202/trackServiceHub").build();

connection.start().then(function () {
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("ReceiveAllVehicleData", function (result) {
    var li = document.createElement("li");
    li.textContent = result;
    document.getElementById("messagesList").appendChild(li);
});

connection.on("ReceiveInstitutionData", function (result) {
    var li = document.createElement("li");
    li.textContent = result;
    document.getElementById("messagesList").appendChild(li);
});

connection.on("ReceiveVehicleData", function (result) {
    var li = document.createElement("li");
    li.textContent = result;
    document.getElementById("messagesList").appendChild(li);
});

connection.on("CommonMessage", function (result) {
    alert(result);
});

connection.onclose(e => {
    console.log('{ "code": "108", "message": "Server connection failed!" }')
});

function setIntervalX(callback, delay, repetitions) {
    var x = 0;
    var intervalID = window.setInterval(function () {

        callback();

        if (++x === repetitions) {
            window.clearInterval(intervalID);
        }
    }, delay);
}

setIntervalX(function () {
    if (connection.state == 'Disconnected') {
        connection.start().then(function () {
            var functionName = document.getElementById("SubscribedFunction").value;
            var value = document.getElementById("SubscribedValue").value;
            if (functionName == 'SubscribeAllVehicle') {
                connection.invoke("Subscribe", null, null, value)
            }
            if (functionName == 'SubscribeInstitutions') {
                connection.invoke("Subscribe", value, null, null)
            }
            if (functionName == 'SubscribeVehicles') {
                connection.invoke("Subscribe", null, value, null)
            }
        }).catch(function (err) {
            return console.error(err.toString());
        });
    }
}, 60000, 4);

document.getElementById("SubscribeAll").addEventListener("click", function (event) {
    connection.invoke("Subscribe", null, null, "--all").catch(function (err) {
        return console.error(err.toString());
    });
    $('#SubscribedFunction').val('SubscribeAllVehicle');
    $('#SubscribedValue').val("--all");
    event.preventDefault();
});

document.getElementById("SubscribeInstitutions").addEventListener("click", function (event) {
    var InstitutionId = document.getElementById("InstitutionId").value;
    connection.invoke("Subscribe", InstitutionId, null, null).catch(function (err) {
        return console.error(err.toString());
    });
    $('#SubscribedFunction').val('SubscribeInstitutions');
    $('#SubscribedValue').val(InstitutionId);
    event.preventDefault();
});

document.getElementById("SubscribeVehicles").addEventListener("click", function (event) {
    var VehicleId = document.getElementById("VehicleId").value;
    connection.invoke("Subscribe", null, VehicleId, null).catch(function (err) {
        return console.error(err.toString());
    });
    $('#SubscribedFunction').val('SubscribeVehicles');
    $('#SubscribedValue').val(VehicleId);
    event.preventDefault();
});

document.getElementById("Unsubscribe").addEventListener("click", function (event) {
    connection.invoke("Unsubscribe").catch(function (err) {
        return console.error(err.toString());
    });
    $('#SubscribedFunction').val('');
    $('#SubscribedValue').val('');
    event.preventDefault();
});