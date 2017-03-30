var connection = new WebSocketManager.Connection("wss://localhost:8081/packet");
var textArea = document.getElementById("packetLog");
var showIncoming = document.getElementById("showIncoming");
var showOutgoing = document.getElementById("showOutgoing");

connection.enableLogging = false;

connection.connectionMethods.onConnected = () => {
    console.log("You are now connected! Connection ID: " + connection.connectionId);
}

connection.clientMethods["receiveMessage"] = (direction, message) => {
    if (direction == "outgoing" && showOutgoing.checked)
        textArea.value = message + "\r\n" + textArea.value;
    if (direction == "incoming" && showIncoming.checked)
        textArea.value = message + "\r\n" + textArea.value;
};

function insertAtCursor(myField, myValue) {
    //IE support
    if (document.selection) {
        myField.focus();
        sel = document.selection.createRange();
        sel.text = myValue;
    }
    //MOZILLA and others
    else if (myField.selectionStart || myField.selectionStart == '0') {
        var startPos = myField.selectionStart;
        var endPos = myField.selectionEnd;
        myField.value = myField.value.substring(0, startPos)
            + myValue
            + myField.value.substring(endPos, myField.value.length);
        myField.selectionStart = startPos + myValue.length;
        myField.selectionEnd = startPos + myValue.length;
    } else {
        myField.value += myValue;
    }
}

connection.start();