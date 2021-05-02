


(testNotificationClick = function () {
  if (document.getElementById('notifyButton') === null || document.getElementById('districtId') === null || document.getElementById('districtId').value =='') {
    return;
  }
 
  if (!('Notification' in window)) {
    console.log('Web Notification not supported');
    return;
  }

  Notification.requestPermission(function (permission) {
    var notification = new Notification("District Open", { body: 'HTML5 Web Notification API', icon: 'http://i.stack.imgur.com/Jzjhz.png?s=48&g=1', dir: 'auto' });
    //setTimeout(function () {
    //  notification.close();
    //}, 3000);
  });  

})();
