var BaseBookModel = function () {
    this.Author = "";
    this.BookId = 0;
    this.Description = "";
    this.ISBN = "";
    this.Name = "";
    this.Category = { CategoryId: 0, CategoryName: '' };
    //this.TagsCollection = [];
};

var Category = function (name, id) {
    this.CategoryName = name;
    this.CategoryId = id;
};

var BooksViewModel = function () {
    var self = this;
    self.lines = ko.observableArray();
    self.iconType = ko.observable("");
    self.searchKey = ko.observable("");
    self.currentPage = ko.observable();
    self.isNewLine = ko.observable(true);
    self.selectedLine = ko.observable();
    self.selectedIssuedBook = ko.observable();
    self.allCategories = ko.observableArray();
    self.userBookCollection = ko.observableArray();

    self.searchByKey = function () {
        self.searchKey($("#search-book-input").val());
    };

    self.pagingSorting = {};
    self.pagingSorting.parameters = {
        PageSize: ko.observable(10),
        CurrentPageIndex: ko.observable(0),
        SortType: ko.observable("asc"),
        CurrentColumn: ko.observable("")
    };
    self.pagingSorting.totalLines = ko.observable(0);
    self.pagingSorting.totalPages = ko.computed(function () {
        return Math.ceil(self.pagingSorting.totalLines() / self.pagingSorting.parameters.PageSize());
    });
    self.pagingSorting.previousPage = function () {
        if (self.pagingSorting.parameters.CurrentPageIndex() > 0) {
            self.pagingSorting.parameters.CurrentPageIndex(self.pagingSorting.parameters.CurrentPageIndex() - 1);
        } else {
            self.pagingSorting.parameters.CurrentPageIndex((Math.ceil(self.pagingSorting.totalLines() / self.pagingSorting.parameters.PageSize())) - 1);
        }
    };
    self.pagingSorting.nextPage = function () {
        if (((self.pagingSorting.parameters.CurrentPageIndex() + 1) * self.pagingSorting.parameters.PageSize()) < self.pagingSorting.totalLines()) {
            self.pagingSorting.parameters.CurrentPageIndex(self.pagingSorting.parameters.CurrentPageIndex() + 1);
        } else {
            self.pagingSorting.parameters.CurrentPageIndex(0);
        }
    };
    self.pagingSorting.changePageSize = function (data, event) {
        var newPageSize = parseInt($(event.target).attr("data-size"), 10);
        if (((self.pagingSorting.parameters.CurrentPageIndex()) * newPageSize) >= self.pagingSorting.totalLines()) {
            self.pagingSorting.parameters.CurrentPageIndex(0).PageSize(newPageSize);
        } else {
            self.pagingSorting.parameters.PageSize(newPageSize);
        }
    };

    self.pagingSorting.sortTable = function (data, event) {
        var sortBy = event.target.nodeName.toLowerCase() == "i" ?
            $(event.target).parent().attr("data-model-property") :
            $(event.target).attr("data-model-property");

        if (sortBy == "do-not-sort") {
            return;
        }

        if (self.pagingSorting.parameters.CurrentColumn() == sortBy) {
            if (self.pagingSorting.parameters.SortType() == 'asc') {
                self.iconType('glyphicon glyphicon-chevron-down');
                self.pagingSorting.parameters.SortType('desc').CurrentColumn(sortBy);
            } else {
                self.iconType('glyphicon glyphicon-chevron-up');
                self.pagingSorting.parameters.SortType('asc').CurrentColumn(sortBy);
            }
        } else {
            self.pagingSorting.parameters.SortType("asc").CurrentColumn(sortBy);
            self.iconType('glyphicon glyphicon-chevron-up');
        }
    };

    var getLines = function () {
        $.ajax({
            url: "/api/LibraryBooks/GetBooks?paging=" + ko.toJSON(self.pagingSorting.parameters) + '&search=' + self.searchKey(),
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.pagingSorting.totalLines(data.TotalLines);
            self.lines(JSON.parse(data.ResultLines));
            return data.ResultBooks;
        });
    };

    self.saveLine = function (formElement) {
        $(formElement).validate();
        if ($(formElement).valid()) {
            var cat;
            for (var i = 0; i < self.allCategories().length; i++) {
                if (self.allCategories()[i].CategoryId == self.selectedLine().Category.CategoryId) {
                    cat = new Category(self.allCategories()[i].CategoryName, self.selectedLine().Category.CategoryId);
                    break;
                }
            }

            var catsToAdd = [];
            for (i = 0; i < self.allCategories().length; i++) {
                if (self.allCategories()[i].CategoryId == -1) {
                    catsToAdd.push(self.allCategories()[i]);
                }
            }
            if (cat) {
                $.ajax({
                    dataType: 'json',
                    type: 'post',
                    url: '/api/LibraryBooks/SaveBook?book=' + ko.toJSON({
                        Author: self.selectedLine().Author,
                        BookId: self.selectedLine().BookId,
                        Description: self.selectedLine().Description,
                        ISBN: self.selectedLine().ISBN,
                        Name: self.selectedLine().Name,
                        Category: cat
                    }) + '&categories=' + ko.toJSON(catsToAdd)
                }).done(function (resp) {
                    $("#add-book-modal").modal('hide');
                    formElement.reset();
                    getLines();
                    self.getAllCategories();
                });
            }
        }
    };

    self.getAllCategories = function() {
        $.ajax({
            dataType: 'json',
            type: 'GET',
            url: '/api/LibraryBooks/GetBookCategories'
        }).done(function (response) {
            //ko.utils.arrayPushAll(self.allCategories, response);
            self.allCategories(response);
        });
    };

    self.currentPage = ko.computed(function () {
        getLines();
    }).extend({ rateLimit: 50 });

    self.editLine = function (data) {
        var d;
        if (data) {
            d = data;
            self.isNewLine(false);
        } else {
            d = new BaseBookModel();
            self.isNewLine(true);
        }

        //$("#tags").tagsinput(d.TagsCollection);
        self.selectedLine(d);
    };

    self.deleteLine = function (bookData) {
        if (confirm("Are you sure you want tot delete this book? \n " + bookData.Name)) {
            var bookId = bookData.BookId;
            if (bookId) {
                $.ajax({
                    url: '/api/LibraryBooks/DeleteBook/' + bookId,
                    type: 'DELETE',
                }).done((function () {
                    if (self.pagingSorting.totalLines() - 1 == 0) {
                        getLines();
                    } else {
                        if (((self.pagingSorting.parameters.CurrentPageIndex()) * self.pagingSorting.parameters.PageSize()) >= self.pagingSorting.totalLines() - 1) {
                            self.pagingSorting.parameters.CurrentPageIndex(0);
                        } else {
                            getLines();
                        }
                    }
                }));
            }
            return true;
        } else {
            return false;
        }
    };

    self.getUserBookCollection = function () {
        $.ajax({
            url: "/api/LibraryBooks/GetUserBooks",
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.userBookCollection(data);
        });
    };

    self.issueTheBook = function (data) {
        $.ajax({
            url: "/api/LibraryBooks/IssueTheBook?bookId=" + data.BookId + "&reserveId="+ data.ReserveId,
            type: "POST",
            datatype: 'json'
        }).done(function () {
            self.getUserBookCollection();
        });
    };

    self.returnTheBook = function () {
        if (self.selectedIssuedBook().Status == '2') {
            $("#custom-validation").html("Please, select new status");
        } else {
            $.ajax({
                url: "/api/LibraryBooks/ReturnTheBook?bookId=" + self.selectedIssuedBook().BookId + "&reserveId="
                    + self.selectedIssuedBook().ReserveId + "&newSatus=" + self.selectedIssuedBook().Status,
                type: "POST",
                datatype: 'json'
            }).done(function () {
                $("#ret-book-modal").modal('hide');
                self.getUserBookCollection();
            });
        }
    };

    self.addCategory = function() {
        var newCatName = $("#newcat").val();
        if (newCatName == "") {
            alert("Please enter category name");
        } else {
            var found = false;
            for (var i = 0; i < self.allCategories().length; i++) {
                if (self.allCategories()[i].CategoryName.toLowerCase() == newCatName.toLowerCase()) {
                    found = true;
                    break;
                }
            }
            if (found) {
                alert("There is already exists category " + newCatName);
            } else {
                self.allCategories.push(new Category(newCatName, -1));
                $("#newcat").val('');
                $('#new-category').hide();
            }
        }
    };
};