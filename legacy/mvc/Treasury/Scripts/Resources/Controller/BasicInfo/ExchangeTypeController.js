var app = angular.module('ExchangeApp', []);
app.controller("ExchangeCtrl", function ($scope, $http) {

    getExchangeTypeLs();

    //To Get All Records 
    function getExchangeTypeLs() {
        var getData = $http.get("/BasicInfo/ExchangeType/List1");
        getData.then(function (exch) {
            $scope.ExchangeTypeLs = exch.data;
            angular.forEach($scope.ExchangeTypeLs, function (obj) {
                obj["showEdit"] = true;
          
            })
        }, function () {
            alert('Error in getting records');
        });
        //edit Row
        $scope.toggleEdit = function (exch) {
            exch.showEdit = exch.showEdit ? false : true;        
        }
        $scope.Save = function (exch) {
            var exchageType = {
                ExchangeTypeID:exch.ExchangeTypeID,
                ExchangeTypeDescription: exch.ExchangeTypeDescription,
                ExchangeTypeDescription2: exch.ExchangeTypeDescription2
            };
            var   getData= $http({
                method: "post",
                url: "/BasicInfo/ExchangeType/Edit1",
                data: JSON.stringify(exchageType),
                dataType: "json"
            });
            getData.then(function (Response) {
                getExchangeTypeLs();
                alert(Response.data.message);
            }, function () {
                alert('Error in updating record');
            });
        }
        $scope.delete = function (exch)
        {
            var getData = $http({
                method:"post",
                url: "/BasicInfo/ExchangeType/Delete1",
                params: {
                    ExchangeTypeID: JSON.stringify(ExchangeTypeID)
                }
            });
            Console.log(exch);
           
        }
        $scope.Insert=function
    }   
});
