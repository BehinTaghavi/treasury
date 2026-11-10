app.directive('dateP', function() {
    return {
        restrict: 'A',
        require: 'ngModel',

        link: function (scope, element, attr, ngModel) {
            element.datepicker({
                format: 'yyyy/mm/dd',
                autoclose: true
            });
            element.next().bind('click',function(){
                element.datepicker('show');
            })
        }
    }
});
