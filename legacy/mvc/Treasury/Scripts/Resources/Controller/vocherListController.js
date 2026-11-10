app.controller('vocherListController', ["$scope", "$http", "$sce", "$location", "$rootScope", "$uibModal", "pettyCashSharedProperties", function ($scope, $http, $sce, $location, $rootScope, $uibModal, pettyCashSharedProperties) {
    $scope.ucno = "";
    $scope.ucname = "";
    $scope.dt;
    $scope.dt1;
    $scope.submitDisable = false;
    $scope.deletedItem = [];
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
    $scope.tst = 0;
    $scope.ShowVocherList = function (pageIndex) {
        var _t = setTimeout(function () {
            $http.get("/Management/Vocher_Row/VocherList?IsPayment=" +
                $scope.IsPayment +
                "&StatusID=" + $scope.StatusID +
                "&requestId=" + $scope.requestId +
                "&IsReturend=" + $scope.IsReturend).then(function (response) {
                    $scope.model = response.data;
                    $scope.tst = 1;
                    $scope.vocherList = response.data.VocherLs;
                    $scope.AccVoucherNo = response.data.AccVoucherNo;
                    $scope.DontSendVoucherDaily = response.data.DontSendVoucherDaily;
                    
                    if (response.data.DontSendVoucherDaily) {
                        $scope.SendVoucherDaily = false;
                    }
                    else {
                        $scope.SendVoucherDaily = true;
                    }
                    $scope.AccountNumLs = response.data.AccountNumLs;
                    if (true) {
                        angular.forEach($scope.vocherList, function (element) {
                            if (element.Unique_Account_No) {
                                element.Unique_Account_No_Title = element.Unique_Account_No_Title.trim() != "" ? element.Unique_Account_No_Title.trim() : null;
                                element.Unique_Account_No = element.Unique_Account_No.trim() != "" ? element.Unique_Account_No.trim() : null;
                                element["UniqueAccount1"] = { Name: element.Unique_Account_No_Title, Code: element.Unique_Account_No };
                            }
                            else {
                                element["UniqueAccount1"] = { Name: null, Code: null };
                            }
                            if (element.Unique_Account_No_two) {
                                element.Unique_Account_No_two_Title = element.Unique_Account_No_two_Title.trim() != "" ? element.Unique_Account_No_two_Title.trim() : null;
                                element.Unique_Account_No_two = element.Unique_Account_No_two.trim() != "" ? element.Unique_Account_No_two.trim() : null;

                                element["UniqueAccount2"] = { Name: element.Unique_Account_No_two_Title, Code: element.Unique_Account_No_two };
                            }
                            else {
                                element["UniqueAccount2"] = { Name: null, Code: null };
                            }





                        })
                    }

                });
        }, 10)

    }
    $scope.getUniqueAccount = function (item) {
        return $scope.model.AccountNumLs.filter(function (el) {
            return el.account_code == item;
        })[0]
    }
    $scope.ShowVocherList(0);
    $scope.addVocherRow = function (item) {
        $scope.vocherList.push(
            {
                AccVoucherNo: null,
                Editable: true,
                Account_No: "",
                Credit: 0,
                requestId: $scope.requestId,
                IsPayment: $scope.IsPayment,
                StateDate: $scope.Date_Field,
                Debit: 0,
                Description: "",
                RowId: 0,
                IsReturend: $scope.IsReturend,
                UniqueAccount1: { Name: null, Code: null },
                UniqueAccount2: { Name: null, Code: null },
                value_Field: $scope.value_Field
            }
        )
    }
    $scope.copyItem = function (item) {
  
        var newItem = angular.copy(item);
        newItem.AccVoucherNo = null;
        newItem.Editable = true;
        newItem.RowId = 0;
        $scope.vocherList.push(newItem);
    }
    $scope.removeItem = function (item) {
        if (item.RowId)
            $scope.deletedItem.push(item);
        var index = $scope.vocherList.indexOf(item);
        $scope.vocherList.splice(index, 1);
    }
    $scope.sumDebit = function () {
        var sum = 0;
        angular.forEach($scope.vocherList, function (item) {
            sum += parseInt(item.Debit);
        });
        return sum;
    }
    $scope.sumCredit = function () {
        var sum = 0;
        angular.forEach($scope.vocherList, function (item) {
            sum += parseInt(item.Credit);
        });
        return sum;
    }
    $scope.reset = function () {
        var model = { ReferenceId: $scope.ReferenceId, IsPayment: true, IsReturend: false };
        $http.post("/Management/Vocher_Row/ResetPaymentVocher", model).then(function (response) {
            if (response.data.result) {
                $scope.ShowVocherList(0);
            }
        }
        );
    }




    $scope.save = function () {
        $scope.submitDisable = true;
  
        //$event.currentTarget.setAttribute("disabled", "disabled");
        var model = [];
        angular.forEach($scope.vocherList, function (item) {
       
            model.push({
                AccVoucherNo: item.AccVoucherNo,
                Editable: item.Editable,
                Account_No: item.Account_No,
                Credit: item.Credit,
                requestId: $scope.requestId,
                IsPayment: $scope.IsPayment,
                Date_Field: item.Date_Field,
                StateDate: item.StateDate,
                Debit: item.Debit,
                Description: item.Description,
                RowId: item.RowId,
                IsReturend: $scope.IsReturend,
                Unique_Account_No: item.UniqueAccount1.Code,
                Unique_Account_No_two: item.UniqueAccount2.Code,
                Exchange_Ammount: item.Exchange_Ammount,
                Value_Field: item.Value_Field,
                Receipt_Pay_TypeID: $scope.Receipt_Pay_TypeID,
                BehalfGroupID: $scope.BehalfGroupID,
                StatusID: $scope.StatusID,
                ReferenceId: $scope.ReferenceId,


            })
        })
        if (model.length > 0) {
            $http.post("/Management/Vocher_Row/Save", model).then(function (response) {
                $http.post("/Management/Vocher_Row/Delete", $scope.deletedItem).then(function (result) {
                    if (response.data.result) {
                        $scope.AccVoucherNo = 0;
                        if ($scope.DontSendVoucherDaily) {
                            document.getElementById('SendBtn').disabled = false;
                        }
                        alertify.success(response.data.message);

                    }
                    else {
                        alertify.error(response.data.message);
                    }
                }
                );
                // alert(response.data);
            }
            );
        }
        else {
            //alertify.error("حداقل یک برنامه ریزی انجام دهید.");
            $event.currentTarget.removeAttribute("disabled");
        }
        $scope.submitDisable = false;
    }

    $scope.send = function () {
        var model = { RefId: $scope.ReferenceId, TypeId: $scope.StatusID, IsReceipt: false };
        $http.post("/Management/Vocher_Row/SendVoucherAgain", model).then(function (response) {
            if (response.data.result == true) {
                var m = response.data.message.split(";");
                $scope.AccVoucherNo = m[0];
                document.getElementById('SendBtn').disabled = true;        
                alertify.success(m[1]);
            }
            else {
                alertify.error(response.data.message);
            }
        }
        );
    }
    $scope.sendReturn = function () {
        var model = { requestIdOrStateId: $scope.ReferenceId };
        $http.post("/Management/Vocher_Row/sendVoucherReturn", model).then(function (response) {
            alert(response.data.message);
        }
        );
    }

    $scope.select2Options = {
        dir: "rtl",
        containerCssClass: "form-control",
    };
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
    $scope.EmptyDateValuefield = function (item) {
        item.Value_Field = null;
        item.Date_Field = null;

    }
    $scope.setUC = function () {
        $scope.uc = pettyCashSharedProperties.GetActiveUC();
        $scope.uc.Name = $scope.ucname;
        $scope.uc.Code = $scope.ucno;
        $scope.uc.hasUnique_Account_no = true;
        pettyCashSharedProperties.SetActiveUC($scope.uc);
    }
    //$scope.setChequeOwnerName = function (item) {
    //    item.Cheque_OwnerName = item.UniqueAccount.Name
    //}





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