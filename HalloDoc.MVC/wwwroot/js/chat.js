"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();
let loggedInUserAspId = $('#loggedInUserAspId').val();
let loggedInUserAccountTypeId = 0;
let chatRequestId = 0;


//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (senderAspUserId, message) {

    let currentTime = new Date();
    let timeToShow = `${padTo2Digits(currentTime.getHours())}:${padTo2Digits(currentTime.getMinutes())}`

    let chatDivElement = generateChatDivElement(message, timeToShow, senderAspUserId);

    document.getElementById("messagesList").appendChild(chatDivElement);

});

connection.on("ReceiveGroupMessage", function (senderAspUserId, message, imagePath) {

    let currentTime = new Date();
    let timeToShow = `${padTo2Digits(currentTime.getHours())}:${padTo2Digits(currentTime.getMinutes())}`

    let chatDivElement = generateGroupChatDivElement(message, timeToShow, imagePath,senderAspUserId);

    document.getElementById("messagesList").appendChild(chatDivElement);

});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {

    let receiverAspUserId = document.getElementById("chatUserAspId").value;
    let message = document.getElementById("chatMessageInput").value;
    let requestId = document.getElementById("chatRequestId").value;

    if (!message.trim()) {
        toastr.error('please input message');
        return;
    }

    $('#chatMessageInput').val('');

    connection.invoke("SendMessage", receiverAspUserId, message, requestId).catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
});


document.getElementById("sendGroupMsgButton").addEventListener("click", function (event) {

    let message = document.getElementById("chatMessageInput").value;
    let requestId = document.getElementById("chatRequestId").value;

    if (!message.trim()) {
        toastr.error('please input message');
        return;
    }

    $('#chatMessageInput').val('');

    connection.invoke("SendMessageToGroup", message, requestId).catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
});


function padTo2Digits(number) {
    let formattedNumber = number.toLocaleString('en-US', {
        minimumIntegerDigits: 2,
        useGrouping: false
    });

    return formattedNumber;
}


$("#chatMessageInput").on("keyup", function (e) {

    if (e.key === 'Enter' || e.keyCode === 13) {

        let isGroupChatVisible = $('#sendGroupMsgButton').is(":visible");
        if (isGroupChatVisible) {
            $('#sendGroupMsgButton').trigger('click');
        }
        else {
            $('#sendButton').trigger('click');
        }
    }
});

function setUpAndShowChatCanvas(requestId, userAspId, accountTypeId) {

    let chatOffCanvas = document.getElementById('Chat')

    let bsOffcanvas = new bootstrap.Offcanvas(chatOffCanvas);
    bsOffcanvas.hide();

    $('#chatUserAspId').val(userAspId);
    $('#chatRequestId').val(requestId);
    chatRequestId = requestId;


    if (accountTypeId == -1) {

        $('#sendButton').hide();
        $('#chat-type-pill').hide();
        $('#sendGroupMsgButton').show();

        $('#chat-user-name').text(`Group for Request ${requestId}`);
        $('#chat-user-profile-image').attr('src', '/images/default/group_default_svg.svg');

        bsOffcanvas.show();
        startFetchingGroupChat();
        return;
    }


    $('#sendButton').show();
    $('#chat-type-pill').show();
    $('#sendGroupMsgButton').hide();

    $.ajax({
        url: "/Guest/GetNameAndImageFromAspId",
        type: 'POST',
        data: {
            userAspId: userAspId,
            accountType: accountTypeId,
        },
        success: function (result) {
            console.log(result);

            if (result["isSuccess"]) {
                $('#chat-user-name').text(result["userName"]);
                $('#chat-user-profile-image').attr('src', result["userImagePath"]);

                bsOffcanvas.show();
                startFetchingChat();
            }
            else {
                bsOffcanvas.hide();
            }
        },
        error: function (error) {
            console.log(error);
            bsOffcanvas.hide();
            alert('error fetching details')
        },
    });

}

function startFetchingGroupChat() {

    document.getElementById("messagesList").innerHTML = "";

    //let chatDivElement = generateGroupChatDivElement("hi", "bye", "/images//default/group_default_svg.svg","");

    //let receiverAspUserId = document.getElementById("chatUserAspId").value;
    //let requestId = document.getElementById("chatRequestId").value;

    $.ajax({
        url: "/Guest/FetchGroupChats",
        data: {
            requestId: chatRequestId,
        },
        type: 'GET',
        success: function (result) {

            $.each(result, function (index, object) {

                let message = object["messageContent"];
                let sentTime = object["sentTime"];
                let senderAspId = object["senderAspId"];
                let imagePath = object["imagePath"];

                //let chatDivElement = generateChatDivElement(message, sentTime, senderAspId);
                let chatDivElement = generateGroupChatDivElement(message,sentTime,imagePath,senderAspId);

                document.getElementById("messagesList").appendChild(chatDivElement);

            });

        },
        error: function (error) {
            console.log(error);
            alert('Error Cancelling Request')
        },
    });
}

function generateGroupChatDivElement(message, sentTime, imagePath, senderAspId) {


    if (senderAspId === "You" || senderAspId === loggedInUserAspId) {
        return generateChatDivElement(message,sentTime,senderAspId);
    }

    let groupChatDivElement = document.createElement("div");

    let chatProfileDivElement = document.createElement("div");
    let chatMessageDivElement = document.createElement("div");

    let profileImgElement = document.createElement("img");
    let chatBubbleDivElement = document.createElement("div");
    let chatTimeSpanElement = document.createElement("span");

    groupChatDivElement.appendChild(chatProfileDivElement);
    groupChatDivElement.appendChild(chatMessageDivElement);

    chatProfileDivElement.appendChild(profileImgElement);
    chatMessageDivElement.appendChild(chatBubbleDivElement);
    chatMessageDivElement.appendChild(chatTimeSpanElement);

    groupChatDivElement.classList.add('group-chat-div');

    chatMessageDivElement.classList.add('ms-2');
    chatProfileDivElement.classList.add('group-chat-image-div');
    profileImgElement.classList.add('group-chat-sender-image');

    chatBubbleDivElement.classList.add('chat-bubble');
    chatTimeSpanElement.classList.add('chat-time-span');

    chatBubbleDivElement.textContent = message;
    chatTimeSpanElement.textContent = sentTime;

    profileImgElement.src = imagePath;

    return groupChatDivElement;
}

function generateChatDivElement(message, sentTime, senderAspId) {

    let chatDivElement = document.createElement("div");
    let chatBubbleDivElement = document.createElement("div");
    let chatTimeSpanElement = document.createElement("span");

    chatDivElement.appendChild(chatBubbleDivElement);
    chatDivElement.appendChild(chatTimeSpanElement);

    chatDivElement.classList.add("chat-div");
    chatBubbleDivElement.classList.add('chat-bubble');
    chatTimeSpanElement.classList.add('chat-time-span');

    chatBubbleDivElement.textContent = message;
    chatTimeSpanElement.textContent = sentTime;

    if (senderAspId === "You" || senderAspId === loggedInUserAspId) {
        chatDivElement.classList.add('self-div');
    }

    return chatDivElement;
}



function startFetchingChat() {

    document.getElementById("messagesList").innerHTML = "";

    let receiverAspUserId = document.getElementById("chatUserAspId").value;
    let requestId = document.getElementById("chatRequestId").value;

    $.ajax({
        url: "/Guest/FetchChats",
        data: {
            senderAspId: loggedInUserAspId,
            receiverAspId: receiverAspUserId,
            requestId: requestId,
        },
        type: 'GET',
        success: function (result) {

            $.each(result, function (index, object) {

                let message = object["messageContent"];
                let sentTime = object["sentTime"];
                let senderAspId = object["senderAspId"];

                let chatDivElement = generateChatDivElement(message, sentTime, senderAspId);

                document.getElementById("messagesList").appendChild(chatDivElement);

            });

        },
        error: function (error) {
            console.log(error);
            alert('Error Cancelling Request')
        },
    });

}

// ON CHAT SHOW EVENT LISTENER
$('#Chat').on('show.bs.offcanvas', function (event) {

    console.log('chat show');

});

// ON CHAT HIDE EVENT LISTENER
$('#Chat').on('hide.bs.offcanvas', function () {

    console.log('chat hide');
    document.getElementById("messagesList").innerHTML = "";

});