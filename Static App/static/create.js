const form = document.getElementById("createform");
form.addEventListener("submit",(e) => {
    e.preventDefault();
    var longurl = form.longurl.value;
    var alias = form.alias.value;
    var data = {}
    //my api supports either pre-defined alias, or self generated
    if(alias.length > 1){
        data["alias"] = alias;
        data["url"] = longurl;
    }
    else{
        data["url"] = longurl
    }
    
    //replace with your API endpoint to generate short url
    var baseURL = "<Your rest endpoint>";
    var request = new XMLHttpRequest();

    request.onreadystatechange = function(){
        if (this.readyState == 4 && this.status == 200) {
            console.log(this.responseText);
            try{
                //when result is a json
                var Obj = JSON.parse(this.responseText);
                document.getElementById("return").style.display='';
                document.getElementById("qrcode").style.display='';
                document.getElementById("return").value = window.location.hostname + "/" + Obj.shorturl;
                new QRCode(document.getElementById("qrcode"),window.location.hostname + "/" + Obj.shorturl);
            }
            catch(err){
                //when result is not a json
                document.getElementById("return").style.display='';
                document.getElementById("qrcode").style.display='';
                document.getElementById("return").value = window.location.hostname + "/" + alias;
                new QRCode(document.getElementById("qrcode"),window.location.hostname + "/" + alias);
            }
            
        }
    }
    request.open('POST',baseURL,false)
    //optional, depends on your request
    request.setRequestHeader("Ocp-Apim-Subscription-Key","<API key>")
    request.setRequestHeader("Content-Type","application/json")
    request.send(JSON.stringify(data))
})

//click to copy function
function copyToClipboard(element){
    element.select();
    if(document.execCommand('copy')){
        document.getElementById("return").style.display='';
        document.getElementById("return").style.background='black';
    }
}