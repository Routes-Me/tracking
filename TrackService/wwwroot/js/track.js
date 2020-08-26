//here build a connection
var connection = new signalR.HubConnectionBuilder().withUrl("/trackServiceHub").build();

document.getElementById("sendBtn").disabled = true;


//This method receive the message and Append it to our list 
connection.on("ReceiveMessage", function (user, message) {
    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var encodedMsg = user + " says " + msg;
    var li = document.createElement("li");
    li.textContent = encodedMsg;
    document.getElementById("messagesList").appendChild(li);
});

connection.start().then(function () {
    document.getElementById("sendBtn").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});


//this block of code is used to send message.
document.getElementById("sendBtn").addEventListener("click", function (event) {
    var user = document.getElementById("userName").value;
    var message = document.getElementById("userMessage").value;
    connection.invoke("SendMessage", user, message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});