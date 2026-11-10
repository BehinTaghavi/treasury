app.controller('pettyCash_uc_Controller', function ($scope, pettyCashSharedProperties) {
    $scope.test = "123456";
    $scope.ucno = "";
    $scope.ucname = "";
    //$scope.uc = "";
    $scope.setUC = function () {
        console.log($scope.uc);
        $scope.uc = pettyCashSharedProperties.GetActiveUC();
        $scope.uc.Name = $scope.ucname;
        $scope.uc.Code = $scope.ucno;
        $scope.uc.Id = $scope.ucno;
        $scope.uc.hasUnique_Account_no = true;
        pettyCashSharedProperties.SetActiveUC($scope.uc);
    }
})
.directive('ngUpdateHidden', function () {
    return {
        restrict: 'AE', //attribute or element
        scope: {},
        replace: true,
        require: 'ngModel',
        link: function ($scope, elem, attr, ngModel) {
            $scope.$watch(ngModel, function (nv) {
                elem.val(nv);
            });
            elem.change(function () { //bind the change event to hidden input
                $scope.$apply(function () {
                    ngModel.$setViewValue(elem.val());
                });
            });
        }
    };
});
