app.controller('showAllReceiptListController', ["$scope", "$http", "$sce", "$location", "$rootScope", "$uibModal", "pettyCashSharedProperties", function ($scope, $http, $sce, $location, $rootScope, $uibModal, pettyCashSharedProperties) {
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.dt;
    $scope.dt1;
    $scope.checkedListItem = [];
    $scope.checkedAll = false;
    var d = new Date();
    var today = d.getFullYear() + "-" + parseInt(d.getMonth() + 1) + "-" + d.getDate();
    $scope.searchModel = {
        UniqueAccount: { Name: "", Code: "" },
        DueDateFrom: "",
        DueDateTo: "",
        RecieptDateFrom: "",
        RecieptDateTo: "",
        ReceiptDetailId: "",
        ChequeNo: "",
        Unique_Account_No: "",
        PageIndex: 0,
        BehalfGroupId: null,
        BehalfID: null,
        Refrence: null,
        FiscalYear: moment(today, 'YYYY-M-D').endOf('jMonth').format('jYYYY')

    }

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
    $scope.ShowReceipt = function (pageIndex) {




        $scope.searchModel.PageIndex = pageIndex;
        $scope.searchModel.Unique_Account_No = $scope.searchModel.UniqueAccount.Code;
        $http.post("/Payment/Receipt/GetAllReceiptList", $scope.searchModel).then(function (response) {
            $scope.receiptList = response.data.List;
            $scope.BehalfGroupLs = response.data.BehalfGroupLs;
            $scope.searchModel.FiscalYear = response.data.FiscalYear;
            $scope.bigTotalItems = response.data.Paging.TotalItemCount;
            $scope.maxSize = response.data.Paging.PageSize;
        });
    }
    $scope.GetReceiptPlanList = function (item) {
        $http.get("/Payment/Receipt/GetReceiptPlanStateList?ReceiptDetailId=" + item.ReceiptDetailId).then(function (response) {
            item.receiptPlanList = response.data;
            item.showReceiptPlanList = true;
        });
    }


    $scope.showVocherList = function (ReceiptDetailId, StateID, PlaningTypeID) {
        $http.get("/Management/Vocher_Row/ShowVocherListReceipt?ReceiptDetailId=" +
            ReceiptDetailId +
            "&StateId=" + StateID + "&PlaningTypeID=" + PlaningTypeID).then(function (response) {
                $scope.thisCanBeusedInsideNgBindHtml = $sce.trustAsHtml(response.data)
                $(".modal.lg.al.ang").find(".modal-title").html("نمایش سند");
                $(".modal.lg.al.ang").modal('show');


                //$scope.vocherList = response.data.VocherLs;
                //$scope.AccountNumLs = response.data.AccountNumLs;
            });
    }


    $scope.ShowReceiptDetails = function (ReceiptDetailId) {
        $http.get("/Payment/Receipt/ShowReceiptDetails?ReceiptDetailsId=" + ReceiptDetailId).then(function (response) {
            $scope.thisCanBeusedInsideNgBindHtml = $sce.trustAsHtml(response.data)
            $(".modal.lg.al.ang").find(".modal-title").html("جزییات");
            $(".modal.lg.al.ang").modal('show');
        });
    }
    $scope.checkAccess = function (ReceiptDetailId) {
        $http.get("/payment/receipt/checkaccess?ReceiptDetailId=" + ReceiptDetailId).then(function (response) {
            $scope.CanDeleteReceiptDetail = response.data.CanDeleteReceiptDetail;
            $scope.CanEditReceiptDetail = response.data.CanEditReceiptDetail;            
            $scope.CanDeleteReceiptDetailStates = response.data.CanDeleteReceiptDetailStates;  
        });
    }
   
    $scope.DeleteReceiptDetails = function (ReceiptDetailId, ReferenceReceiptId) {
        //alertify.alert("آیا برای حذف مطمین هستید؟", function () { });
        var hasrelated = false;
        var message = 'آیا برای حذف مطمین هستید؟';
        if (ReferenceReceiptId == 1) {
            message = 'این چک با چک های دیگر وابستگی دارد درصورت حذف همه حذف می گردد!';
            hasrelated = true;
        }

        alertify.confirm('حذف', message, function () {
            $http.get("/Payment/Receipt/Delete?ReceiptDetailId=" + ReceiptDetailId).then(function (response) {
                if (response.data.result == true) {
                    alertify.success(response.data.message);
                }
                else {
                    alertify.error(response.data.message);
                }
                setTimeout(function () {
                    location.reload();
                }, 2000)
            });
            //$http.get("/Payment/Receipt/Delete?ReceiptDetailId=" + ReceiptDetailId).then(function (response) {
            //    setTimeout(function () {
            //        location.reload();
            //    }, 2 )
            //});

        }, function () { alertify.error('انصراف') });
    }

    $scope.DeleteReceiptDetailsPlanState = function (StateID, hasVoucher) {
        var message = 'آیا برای حذف وضعیت مطمین هستید؟';
        if (hasVoucher != 0) {
            message += 'درصورت حذف سند ' + hasVoucher + ' دردفترداری حذف خواهد شد';
        }
        alertify.confirm('حذف', message, function () {
            $http.get("/Payment/Receipt/DeleteReceiptDetailsPlanState?StateID=" + StateID).then(function (response) {
                if (response.data.result == true) {
                    alertify.success(response.data.message);
                }
                else {
                    alertify.error(response.data.message);
                }
                setTimeout(function () {
                    location.reload();
                }, 2000)
            });
        }, function () { alertify.error('انصراف') });
    }
    $scope.DeleteALLReceiptDetailsPlanState = function (ReceiptDetailId, hasVoucher) {

        alertify.confirm('حذف', 'آیا برای حذف وضعیت ها مطمین هستید؟', function () {
            $http.get("/Payment/Receipt/DeleteALLReceiptDetailsPlanState?ReceiptDetailId=" + ReceiptDetailId).then(function (response) {
                if (response.data.result == true) {
                    alertify.success(response.data.message);
                }
                else {
                    alertify.error(response.data.message);
                }
                setTimeout(function () {
                    location.reload();
                }, 2000)
            });
        }, function () { alertify.error('انصراف') });
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
        var result = false;
        angular.forEach($scope.receiptList, function (element, i) {
            if (element.checked) {
                result = true;
                return;
            }
        });
        return result;
    }
    $scope.save = function ($event) {
        $event.currentTarget.setAttribute("disabled", "disabled");
        var model = [];
        angular.forEach($scope.receiptList, function (item) {
            if (item.checked) {
                model.push({
                    chequeId: 0,
                    ChequeState: item.ChequeState,
                    ReceiptDetailId: item.ReceiptDetailId,
                    PlaningTypeValue: item.PlaningTypeValue,
                    BankId: item.BankId,
                    unique_account1: item.UniqueAccount1.Code,
                    unique_account2: item.UniqueAccount2.Code,
                    DueDate: item.DueDate,
                    BehalfGroupID: item.BehalfGroupID,
                    BehalfID: item.BehalfID,
                    CashierID: item.CashierID,
                    State: $scope.State,

                })
            }
        })
        if (model.length > 0) {
            $http.post("/Payment/Receipt/SavePlanState", model).then(function (response) {
                if ($scope.State == 1)
                    alertify.success("واگذاری چک های دریافتی به حساب بانک با موفقیت انجام شد.");
                if ($scope.State == 2)
                    alertify.success("ثبت وصول / برگشت / عودت چک های واگذاری با موفقیت انجام شد");
                if ($scope.State == 3)
                    alertify.success("ثبت خرج چک های دریافتی با موفقیت انجام شد");
                if ($scope.State == 4)
                    alertify.success("ثبت عودت چک های خرج شده با موفقیت انجام شد");
                setTimeout(function () {
                    location.reload();
                }, 2000)
            }

            );
        }
        else {
            alertify.error("حداقل یک برنامه ریزی انجام دهید.");
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

        $http.get("/Payment/Receipt/GetBehalfLs?BehalfGroupId=" + item).then(function (response) {

            $scope.BehalfList = response.data.BehalfList;
            $scope.Refrences = response.data.Refrences;
        });
    }

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
            $scope.thisCanBeusedInsideNgBindHtml_acc = $sce.trustAsHtml(response.data)
            $(".modal.md.ang.acc").find(".modal-title").html("انتخاب تفصیل");
            $(".modal.md.ang.acc").modal('show');
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
    //    $scope.checkedListItem = $scope.receiptList.map(function (item) { return item.ReceiptDetailId; });
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
        angular.forEach($scope.receiptList, function (item) {
            price += item.Price;
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
    })
    .service('pettyCashSharedProperties', function () {
        var activeUC = { Code: 0, Name: "" };
        return {
            GetActiveUC: function () {
                return activeUC;
            },
            SetActiveUC: function (uc) {
                activeUC = uc;
            }
        };
    })
    .filter('filterReceiptList', function () {
        return function (items) {
            var filtered = [];
            angular.forEach(items, function (item) {
                if (item.DueDate && new Date(item.DueDate_en) <= new Date()) {
                    filtered.push(item);
                }
                else if (!item.DueDate) {
                    filtered.push(item);
                }
            });
            return filtered;
        };
    });