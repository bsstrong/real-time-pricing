<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="com.LoanTek.API.Leads.Facebook.WebForm1" Async="true" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <script src="https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js"></script>
    <title></title>

<style>
    .pageTable {
        border:none;
        padding-left:10px;
    }
    .pageTable td {
        padding-left:10px;
    }

</style>
<script>
    var userID, pageName, pageID;
    var pageArray;
    var FBPages;
    var appID = '210524392743243'; // '884623964986158';

    var encryptedClientID = "GxtJglv5AbwA";

    window.onload = function () {
        console.log('FB init');
        window.fbAsyncInit();
        GetPageSubscriptions(false);
    };

    window.fbAsyncInit = function () {
    FB.init({
      appId      : appID,
      xfbml      : true,
      version    : 'v2.5'
    });
  };

  (function(d, s, id){
     var js, fjs = d.getElementsByTagName(s)[0];
     if (d.getElementById(id)) {return;}
     js = d.createElement(s); js.id = id;
     js.src = "//connect.facebook.net/en_US/sdk.js";
     fjs.parentNode.insertBefore(js, fjs);
   }(document, 'script', 'facebook-jssdk'));

  function subscribeApp(page_id, page_access_token, page_name) {
    console.log('Subscribing page to app! ' + page_id);
    FB.api(
      '/' + page_id + '/subscribed_apps',
      'post',
      {access_token: page_access_token},
      function(response) {
          console.log('Successfully subscribed page', response);
          console.log('page_access_token');
          PostPageSubscription(page_name, page_id, page_access_token);
    });
  }

  function myFacebookLogin() {
      // Only works after `FB.init` is called
      FB.login(function (response) {
          console.log('Successfully logged in', response);
          getFBpages();
      }, { scope: 'manage_pages' });
  }

  function getFBpages()
  {
      FB.api('/me/accounts', function (response) {
          $("#btnFB").hide();
          console.log('Successfully retrieved pages', response);
          var pages = response.data;
          FBPages = pages;
          var ul = document.getElementById('list');
          var alreadySubscribed;

          $("#FBPages").html("<b>Available Pages</b>");

          var table = $('<table></table>').addClass('pageTable');
          var addedAny = false;
          for (var i = 0, len = pages.length; i < len; i++) {
              var page = pages[i];

              var row = $('<tr></tr>');
              var cell = $('<td></td>').text(page.name);
              row.append(cell);

              alreadySubscribed = false;
              $.each(pageArray, function (key, value) {
                  if (value.PageID == page.id) {
                      alreadySubscribed = true;
                      return;
                  }
              });

              if (!alreadySubscribed) {
                  cell = $('<td></td>');
                  cell.append($("<input type='Button' value='Add' onclick='subscribeApp(" + page.id + ",\"" + page.access_token + "\",\"" + page.name + "\");' />"));
                  row.append(cell);
                  table.append(row);
                  addedAny = true;
              }
          }

          if (addedAny)
              $("#FBPages").append(table);
          else
              $("#FBPages").html("<br><b>You don't have any pages that can be added at this time!</b>");

      });
  }

  function getPostURL() {
      return "https://api.loantek.com/Leads.Facebook/api/webhooks/" + encryptedClientID + "/";
      //return "http://localhost:63684/api/webhooks/" + encryptedClientID + "/";  //Debug
  }

  function PostPageSubscription(PageName, PageID, PageAccessToken) {
      console.log('Starting post');

      var postUrl;

      postUrl = getPostURL();

      postUrl += "?Action=ADD&PageName=" + PageName

      postUrl += "&PageID=" + PageID.toString()

      postUrl += "&Token=" + PageAccessToken;

      postUrl = postUrl.replace(' ', '%20');

      $.post(postUrl,
      {
          Action: "Add",
      },
      function(data,status){
          console.log("Data: " + data + "\nStatus: " + status);
          GetPageSubscriptions(true);
          alert('Page Successfully Added');
      });
  }

  function removePage(PageID, PageName) {
      console.log('Starting post');

      var postUrl;

      postUrl = getPostURL();

      postUrl += "?Action=REMOVE&PageName=" + PageName

      postUrl += "&PageID=" + PageID.toString()

      postUrl += "&UserID=0";

      postUrl = postUrl.replace(' ', '%20');

      $.post(postUrl,
      {
          Action: "Remove",
      },
      function (data, status) {
          console.log("Data: " + data + "\nStatus: " + status);
          GetPageSubscriptions(true);
          alert('Page Successfully Removed');
      });
  }

  function GetPageSubscriptions(doFB) {
      console.log('Starting get');

      var postUrl;

      postUrl = getPostURL();

      $("#ExistingPages").html = '';  // No current pages

      $.get(postUrl,
      {
      },
      function (data, status) {
          pageArray = data;
          console.log("Status: " + status);

          $("#ExistingPages").html('');

          if (data != '' && data.length > 0) {

              $("#ExistingPages").append("<b>Currently Subscribed Pages</b>");  //<tr><td><b>Page Name</b></th><th><b>Action</b></td></tr>

              var table = $('<table></table>').addClass('pageTable');

              $.each(data, function (key, value) {
                  var row = $('<tr></tr>');
                  var cell = $('<td></td>').text(value.PageName);
                  row.append(cell);

                  cell = $('<td></td>');
                  cell.append($("<input type='Button' value='Remove' onclick='removePage(" + value.PageID + ", \""+value.PageName+"\");' />"));
                  row.append(cell);

                  table.append(row);
              });
              $("#ExistingPages").append(table);
          }
      });

      if (doFB)
          getFBpages();
  }

</script>


</head>
<body>
<h2>LoanTek Facebook Leads CRM Manager</h2>
Subscribing your pages to our application will allow Facebook lead generation events & lead details to flow directly into the LoanTek CRM.  Your Facebook information won't be used for any other purposes.
        <div id="ExistingPages">

        </div>

    <div id ="btnFB">
        <h4>To subscribe pages, please click the button below to log in to Facebook and grant access</h4>
         <button onclick="myFacebookLogin()">Login with Facebook</button>
    </div>
        <div id="FBPages">
            <ul id="list"></ul>
        </div>
</body>
</html>
