app.controller('aggregationListController', ["$scope", "$http", "$sce", "$location", "$rootScope", "$uibModal", "pettyCashSharedProperties", function ($scope, $http, $sce, $location, $rootScope, $uibModal, pettyCashSharedProperties) {
    var search = location.search.substring(1);
    //$scope.url = (JSON.parse('{"' + search.replace(/&/g, '","').replace(/=/g, '":"') + '"}',
    //                 function (key, value) { return key === "" ? value : decodeURIComponent(value) }));
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.dt;
    $scope.dt1;
    $scope.checkedListItem = [];
    $scope.checkedAll = false;
    $scope.Unique_Account_No = { Name: '', Code: '' };
    $scope.Main_Unique_Account_No = { Name: '', Code: '' };
    $scope.Source_PaymentValue = 0;
    $scope.BehalfId = 0;
    //var receiptItem = {
    //    ReceiptId: 0,
    //    Cheque_Number: "",
    //    openPersian: "",
    //    DueDate: "",
    //    UniqueAccount: { Name: "", Code: 0 },
    //    ChequePrice_Rial: 1000,
    //    ExchangeTypeID: 0,
    //    ExchangeRate: 1,
    //    EquivalencePrice: 0,
    //    BehalfID: 0,
    //    Description: "",
    //    BankID: 0,
    //    isOpen: false,
    //    Bank_AccountNo: "",
    //    Bank_BranchDesc: "",
    //    Bank_BranchCode: "",
    //    CityID: 0

    //}
    $scope.receiptPayTypeList = [
        {
            title: "چک روزي",
            id: 18
        },
        {
            title: "چک مدت دار",
            id: 19
        },
        {
            title: "چک انتظامی",
            id: 21
        }
    ]
    $scope.wordifyfa = function (num, level) {
        'use strict';
        if (num === null) {
            return "";
        }
        // convert negative number to positive and get wordify value
        if (num < 0) {
            num = num * -1;
            return "منفی " + $scope.wordifyfa(num, level);
        }
        if (num === 0) {
            if (level === 0) {
                return "صفر";
            } else {
                return "";
            }
        }
        var result = "",
            yekan = ["یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه"],
            dahgan = ["بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود"],
            sadgan = ["یکصد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد"],
            dah = ["ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هیجده", "نوزده"];
        if (level > 0) {
            result += " و ";
            level -= 1;
        }

        if (num < 10) {
            result += yekan[num - 1];
        } else if (num < 20) {
            result += dah[num - 10];
        } else if (num < 100) {
            result += dahgan[parseInt(num / 10, 10) - 2] + $scope.wordifyfa(num % 10, level + 1);
        } else if (num < 1000) {
            result += sadgan[parseInt(num / 100, 10) - 1] + $scope.wordifyfa(num % 100, level + 1);
        } else if (num < 1000000) {
            result += $scope.wordifyfa(parseInt(num / 1000, 10), level) + " هزار " + $scope.wordifyfa(num % 1000, level + 1);
        } else if (num < 1000000000) {
            result += $scope.wordifyfa(parseInt(num / 1000000, 10), level) + " میلیون " + $scope.wordifyfa(num % 1000000, level + 1);
        } else if (num < 1000000000000) {
            result += $scope.wordifyfa(parseInt(num / 1000000000, 10), level) + " میلیارد " + $scope.wordifyfa(num % 1000000000, level + 1);
        } else if (num < 1000000000000000) {
            result += $scope.wordifyfa(parseInt(num / 1000000000000, 10), level) + " تریلیارد " + $scope.wordifyfa(num % 1000000000000, level + 1);
        }
        return result;

    };
    $scope.wordifyRialsInTomans = function (num) {
        'use strict';
        return $scope.wordifyfa(num, 0);
    };
    $scope.checkUA2 = function (item, state) {
        $http.get("/Payment/Receipt/HasUniqueAccountTwo?ReceiptDetailId=" + item.ReceiptDetailId + "&BankId=" + item.BankId + "&Account_No_Type=" + state + "&State=" + $scope.State).then(function (response) {
            item["hasUniqueAccount2"] = response.data;
        });
    }
    $scope.checkUA = function (item) {
        $http.get("/Payment/Request/HasUniqueAccount?BehalfGroupId=" + item.BehalfGroupID).then(function (response) {
            item["hasUniqueAccount1"] = response.data[0];
            item["hasUniqueAccount2"] = response.data[1];
        });
    }
    //$scope.searchModel = {
    //    RecieptType: {
    //        Receipt_Pay_TypeDetailID: ($scope.url.RecieptTypeId) ? $scope.url.RecieptTypeId : 18,
    //        Receipt_Pay_TypeDetailDescription: "چک روزي"
    //    },
    //    UniqueAccount: { Name: null, Code: null },
    //    State: 0,
    //    DueDateFrom: "",
    //    DueDateTo: "",
    //    RecieptDateFrom: "",
    //    RecieptDateTo: ""
    //}
    $scope.ShowReceipt = function (pageIndex) {
        $http.post("/Payment/Request/ShowAggregationRequest?RecieptTypeId").then(function (response) {
            $scope.aggrigateList = response.data.list;
            $scope.banks = response.data.banks;
            $scope.behlfLs = response.data.behlfLs;
            if (true) {
                angular.forEach($scope.aggrigateList, function (element) {
                    element["UniqueAccount1"] = { Name: null, Code: null };
                    element["UniqueAccount2"] = { Name: null, Code: null }
                })
            }
            //document.getElementById("searchFormButton").disabled = false;
            //document.getElementById("searchFormButton").innerHTML = "جستجو";
        });
    }

    $scope.showVocherList = function (ReceiptDetailId, StateID) {
        $http.get("/Management/Vocher_Row/ShowVocherListReceipt?ReceiptDetailId=" +
               ReceiptDetailId +
               "&StateId=" + StateID).then(function (response) {
                   $scope.thisCanBeusedInsideNgBindHtml = $sce.trustAsHtml(response.data)
                   $(".modal.lg.al.ang").find(".modal-title").html("نمایش سند");
                   $(".modal.lg.al.ang").modal('show');


                   //$scope.vocherList = response.data.VocherLs;
                   //$scope.AccountNumLs = response.data.AccountNumLs;
               });
    }
    $scope.select2Options = {
        dir: "rtl",
        containerCssClass: "form-control",
        //ajax: {
        //    url: "/Payment/Receipt/BanksAccountLS",
        //    dataType: 'json',
        //    delay: 250,
        //    data: function (params) {
        //        params.page = params.page || 1;
        //        return {
        //            pageSize: 1000,
        //            page: params.page,
        //            q: params.term, // search term
        //        };
        //    },
        //    processResults: function (data, params) {
        //        params.page = params.page || 1;
        //        return {
        //            results: data.query,
        //            pagination: {
        //                more: (params.page * 1000 < data.total_count)
        //            }
        //        };
        //    },
        //    cache: true
        //},
        //escapeMarkup: function (markup) { return markup; }, // let our custom formatter work
        //minimumInputLength: 1
    };
    //$scope.$watch('searchModel.UniqueAccount.Code', function (newValue, oldValue, scope) {
    //    if (newValue != oldValue) {
    //        $scope.ShowReceipt(0);
    //    }
    //});
    $scope.$watch('checkAll', function (newValue, oldValue, scope) {
        angular.forEach($scope.aggrigateList, function (item) {
            if ($scope.checkAll) {
                item.checked = true;
            }
            else {
                item.checked = false;
            }
        })
    });
    //$scope.$watch('searchModel.DueDateFrom', function (newValue, oldValue, scope) {
    //    if (newValue != oldValue) {
    //        $scope.ShowReceipt(0);
    //    }
    //});
    //$scope.$watch('searchModel.DueDateTo', function (newValue, oldValue, scope) {
    //    if (newValue != oldValue) {
    //        $scope.ShowReceipt(0);
    //    }
    //});
    //$scope.$watch('searchModel.RecieptDateFrom', function (newValue, oldValue, scope) {
    //    if (newValue != oldValue) {
    //        $scope.ShowReceipt(0);
    //    }
    //});
    //$scope.$watch('searchModel.RecieptDateTo', function (newValue, oldValue, scope) {
    //    if (newValue != oldValue) {
    //        $scope.ShowReceipt(0);
    //    }
    //});
    //$scope.$watch('searchModel.RecieptType.PlaningTypeReferenceID', function (newValue, oldValue, scope) {
    //    if (newValue != oldValue) {
    //        $scope.ShowReceipt(0);
    //    }
    //});
    //$scope.addCheque = function () {
    //    $scope.model.detailsLs.push({
    //        Cheque_Number: "",
    //        DueDate: "",
    //        ChequePrice_Rial: 0,
    //        Bank_AccountNo: "",
    //        BankID: 0,
    //        Bank_BranchCode: "",
    //        CityID: 0,
    //        ExchangeRate: 0,
    //        ExchangeTypeID: 0,
    //        BehalfID: 0,

    //        BehalfGroupId: 0,
    //        UniqueAccount: {
    //            Code: 0,
    //            Name: ""
    //        }
    //    })
    //}

    $scope.hasChecked = function () {
        var count = 0;
        angular.forEach($scope.aggrigateList, function (element, i) {
            if (element.checked) {
                count++;
            }

        })
        if (count >= 1) {
            return true;
        }
        else {
            return false;
        }
    }
    $scope.save = function ($event) {
        $event.currentTarget.setAttribute("disabled", "disabled");
        var requestIds = [];
        angular.forEach($scope.aggrigateList, function (item) {
            if (item.checked) {
                requestIds.push(item.RequestID)
            }
        })

        if (requestIds.length > 0) {
            var model = {
                requestIds: requestIds,
                Unique_Account_No: $scope.Unique_Account_No.Code,
                Source_PaymentValue: $scope.Source_PaymentValue,
                Main_Unique_Account_No: $scope.Main_Unique_Account_No.Code,
                BehalfId: $scope.BehalfId,
            }
            $http.post("/Payment/Request/AggregationRequest", model).then(function (response) {
                location.replace("/Payment/Pay?Status=DuringPayment");
            });
        }
        else {
            alertify.error("حداقل دو درخواست باید انتخاب شود.");
            $event.currentTarget.removeAttribute("disabled");
        }

    }
    $scope.loadCity = function (provinceId) {
        $http.get("/Payment/Receipt/GetCityLs?provinceID=" + provinceId).then(function (response) {
            $scope.model.CityLs = response.data;
            //if (response.data.Header != undefined) {
            //    $scope.activeHeader = response.data.Header;
            //}
            ////else {
            ////    $scope.activeHeader = response.data.Header;
            ////}
            //$scope.behalfGroupList = response.data.BehalfGroupList;
            //$scope.accountNumLs = response.data.AccountNumLs;
            //$scope.updateSumPrice()
        });
    }
    $scope.loadBehalf = function (item) {
        //item.Refrences = [];
        $http.get("/Payment/Receipt/GetBehalfLs?BehalfGroupId=" + item.BehalfGroupId).then(function (response) {

            item.BehalfList = response.data.BehalfList;
            item.Refrences = response.data.Refrences;
            //if (response.data.Header != undefined) {
            //    $scope.activeHeader = response.data.Header;
            //}
            ////else {
            ////    $scope.activeHeader = response.data.Header;
            ////}
            //$scope.behalfGroupList = response.data.BehalfGroupList;
            //$scope.accountNumLs = response.data.AccountNumLs;
            //$scope.updateSumPrice()
        });
    }

    //$scope.State = $scope.url.State;
    $scope.ShowReceipt(0);

    $scope.today = function () {
        $scope.dt = new Date();
    };
    $scope.today();
    $scope.clear = function () {
        $scope.dt = null;
    };
    $scope.disabled = function (date, mode) {
        return (mode === 'day' && date.getDay() === 5);
    };
    $scope.toggleMin = function () {
        $scope.minDate = $scope.minDate ? null : new Date();
    };
    $scope.toggleMin();
    $scope.dateOptions = {
        formatYear: 'yyyy',
        startingDay: 8
    };
    $scope.initDate = new Date('2016-15-20');
    $scope.formats = ['dd-MMMM-yyyy', 'yyyy/MM/dd', 'yyyy/MMMM/dd', 'dd.MM.yyyy', 'shortDate'];
    $scope.format = $scope.formats[1];
    $scope.changeUniqueAccount = function (uc) {
        pettyCashSharedProperties.SetActiveUC(uc);
        $http.get("/Payment/PettyCash/getPettyCashUC").then(function (response) {
            $scope.thisCanBeusedInsideNgBindHtml = $sce.trustAsHtml(response.data)
            $(".modal.md.ang").find(".modal-title").html("انتخاب تفصیل");
            $(".modal.md.ang").modal('show');
            $("#UniqueAccountType, #Code, #Name").val('');
        });
    }
    $scope.setUC = function () {
        $scope.uc = pettyCashSharedProperties.GetActiveUC();
        $scope.uc.Name = $scope.ucname;
        $scope.uc.Code = $scope.ucno;
        $scope.uc.hasUnique_Account_no = true;
        pettyCashSharedProperties.SetActiveUC($scope.uc);
    }
    $scope.removeItem = function (item) {
        var index = $scope.model.detailsLs.indexOf(item);
        $scope.model.detailsLs.splice(index, 1);
    }
    $scope.setNowDate = function (state, item) {
        if (state) {
            var d = new Date();
            var today = d.getFullYear() + "-" + parseInt(d.getMonth() + 1) + "-" + d.getDate();
            item.DueDate = moment(today, 'YYYY-M-D').endOf('jMonth').format('jYYYY/jM/jD/');
        }
    }
    //$scope.checkAll = function (e) {
    //    $scope.checkedAll = true;
    //    $scope.checkedListItem = $scope.aggrigateList.map(function (item) { return item.ReceiptDetailId; });
    //};
    //$scope.unCheckAll = function (e) {
    //    $scope.checkedAll = false;
    //    $scope.checkedListItem = [];
    //};
    $scope.setDescription = function (item, title) {
        var desc = alertify.prompt(title, (item.PlaningTypeValue != null) ? item.PlaningTypeValue : "", function (evt, value) { item.PlaningTypeValue = value; $scope.$apply() });
    }
    $scope.showDescription = function (item) {
        alertify.alert(title, item.PlaningTypeValue, function () { });
    }
    //pagination
    //$scope.totalItems = 30;
    $scope.currentPage = 1;

    $scope.setPage = function (pageNo) {
        $scope.currentPage = pageNo;
        $scope.ShowReceipt(pageNo - 1);
    };

    $scope.pageChanged = function () {
        $scope.ShowReceipt($scope.currentPage - 1);


    };
    $scope.sumPrice = function () {
        var price = 0
        angular.forEach($scope.aggrigateList, function (item) {
            if (item.checked) {
                price += item.PayAmount;
            }

        })
        return price;
    }



    $scope.convertCheque = function (item) {
        $scope.open(item, 'sm', 'myModalContent')
    }
    $scope.getChequePaper = function (item) {
        $http.post("/Payment/Receipt/GetChequePaper?ReceiptDetailId=" + item.ReceiptDetailId).then(function (response) {
            alert("لاشه چک تحویل گرفته شد.")
            location.reload();
        });
    }
    //Modal

    var $ctrl = this;
    $scope.items = ['item1', 'item2', 'item3'];

    $scope.animationsEnabled = true;

    $scope.open = function (item, size, url, parentSelector) {
        var parentElem = parentSelector ?
            angular.element($document[0].querySelector('.modal-demo ' + parentSelector)) : undefined;
        var modalInstance = $uibModal.open({
            animation: $scope.animationsEnabled,
            ariaLabelledBy: 'modal-title',
            ariaDescribedBy: 'modal-body',
            templateUrl: url + '.html',
            controller: 'ModalInstanceController',
            controllerAs: '$ctrl',
            size: size,
            appendTo: parentElem,
            resolve: {
                object: function () {
                    return item;
                }
            }
        });

        modalInstance.result.then(function () {
            $http.post("/Payment/Receipt/ConvertCheque?ReceiptDetailId=" + item.ReceiptDetailId + "&DueDate=" + item.DueDate.split("/").join("")).then(function (response) {
                alert("تبدیل چک با موفقیت انجام شد.")
                location.reload();
            });

        }, function () {
            //$log.info('Modal dismissed at: ' + new Date());
        });
    };
    $scope.getNumber = function (num) {
        return new Array(num);
    }
    $scope.getImageUrl = function (image) {
        return 'http://bonmod.ir/Gallery/' + image.gallery + '/lg/' + image.fileName;
    }
    $scope.openComponentModal = function () {
        var modalInstance = $uibModal.open({
            animation: $scope.animationsEnabled,
            component: 'modalComponent',
            resolve: {
                object: function () {
                    return $scope.SelectedProduct;
                }
            }
        });

        modalInstance.result.then(function (selectedItem) {
            $scope.selected = selectedItem;
        }, function () {
            //$log.info('modal-component dismissed at: ' + new Date());
        });
    };

    $scope.openMultipleModals = function () {
        $uibModal.open({
            animation: $scope.animationsEnabled,
            ariaLabelledBy: 'modal-title-bottom',
            ariaDescribedBy: 'modal-body-bottom',
            templateUrl: 'stackedModal.html',
            size: 'sm',
            controller: function ($scope) {
                $scope.name = 'bottom';
            }
        });

        $uibModal.open({
            animation: $scope.animationsEnabled,
            ariaLabelledBy: 'modal-title-top',
            ariaDescribedBy: 'modal-body-top',
            templateUrl: 'stackedModal.html',
            size: 'sm',
            controller: function ($scope) {
                $scope.name = 'top';
            }
        });
    };

    $scope.toggleAnimation = function () {
        $scope.animationsEnabled = !$scope.animationsEnabled;
    };
}])
.service('pettyCashSharedProperties', function () {
    var activeUC = { Code: 0, Name: "" };
    return {
        GetActiveUC: function () {
            return this.activeUC;
        },
        SetActiveUC: function (uc) {
            this.activeUC = uc;
        }
    };
})
.component('modalComponent', {
    templateUrl: 'myModalContent.html',
    bindings: {
        resolve: '<',
        close: '&',
        dismiss: '&'
    },
    controller: function ($scope) {
        //var $ctrl = this;

        $scope.$onInit = function () {
            $scope.items = $scope.resolve.items;
            $scope.selected = {
                item: $scope.items[0]
            };
        };

        $scope.ok = function () {
            $scope.close({ $value: $scope.selected.item });
        };

        $scope.cancel = function () {
            $scope.dismiss({ $value: 'cancel' });
        };
    }
})
.filter('receiptPayTypeDetailsFilter', function () {
    return function (items, state) {
        var filtered = [];
        angular.forEach(items, function (item) {
            if (state != -7 && state != -15 && state != 15) {
                if (item.Receipt_Pay_TypeDetailID == 18 || item.Receipt_Pay_TypeDetailID == 19) {
                    filtered.push(item);
                }
            }
            else {
                if (item.Receipt_Pay_TypeDetailID == 21) {
                    filtered.push(item);
                }
            }
        });
        return filtered;
    };
});