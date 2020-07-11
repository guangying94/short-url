//check authentication status
const url = window.location.protocol + '//' + window.location.hostname + '/.auth/me';
        console.log(url);
        try{
            var request = new XMLHttpRequest();
            request.open('GET',url,false),
            request.send();

            if(request.status == 200){
                console.log(request.responseText);
                var Obj = JSON.parse(request.responseText);
                //check status if user is authenticated
                var status = 'authenticated'
                if(Obj.clientPrincipal.userRoles.indexOf(status) > 0){
                    window.location="/create"
                }
            }
        }
        catch(err){
            console.log(err.message);
        }