var app = angular.module('treasury', ['ngSanitize', 'ui.bootstrap.persian.datepicker', 'ui.bootstrap.datepicker', 'checklist-model', 'ui.bootstrap', 'ui.select2']);
//app.directive('ngDate', function () {
//    return {
//        restrict: 'A',
//        replace: true,
//        require: 'ngModel',
//        scope: {
//            ssn: '=ngModel',
//            disabled: '=ngDisabled'
//        },
//        //templateUrl points to an external html template.
//        templateUrl: '../Scripts/templates/Date.html',
//    }
//});
app.directive('loading', ['$http', function ($http) {
    return {
        restrict: 'A',
        link: function (scope, element, attrs) {
            scope.isLoading = function () {
                return $http.pendingRequests.length > 0;
            };
            scope.$watch(scope.isLoading, function (value) {
                if (value) {
                    $("#ajaxSpinner").show();
                } else {
                    $("#ajaxSpinner").hide();
                }
            });
        }
    }
}]);
app.directive('dateP', function () {
    return {
        restrict: 'A',
        require: 'ngModel',
        scope: {
            maxdate: "@",
            mindate: "@"
        },
        link: function (scope, element, attr, ngModel) {
            element.datepicker({
                format: 'yyyy/mm/dd',
                autoclose: true,
                maxDate: scope.maxdate,
                minDate: scope.mindate
            });
            element.next().bind('click', function () {
                element.datepicker('show');
            })
        }
    }
}).directive('ignoreMouseWheel', function () {
    return {
        restrict: 'A',
        link: function (scope, element) {
            element.bind('mousewheel', function () {
                element[0].blur();
            });
        }
    };
});
app.controller('ModalInstanceController', ["$scope", "$uibModalInstance", "object",
function ($scope, $uibModalInstance, object) {
    //var $ctrl = this;
    $scope.object = object;
    //$scope.selected = {
    //  item: $scope.items[0]
    //};

    $scope.ok = function () {
        $uibModalInstance.close();
    };
    $scope.okay = function () {
        $uibModalInstance.close();
    };

    $scope.cancel = function () {
        $uibModalInstance.dismiss('cancel');
    };

    $scope.print = function () {
        $uibModalInstance.close("printarea");
    };
}]);