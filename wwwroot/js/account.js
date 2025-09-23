let accountStateCheckInterval = null;
let auditSearchTimeout = null;

function reviewPhoto(element, action) {
    var id = element.data('id');
    if (!id) {
        displayMessage(localization.translate('Review'), localization.translate('Review_Id_Missing'));
        return;
    }

    displayLoader(localization.translate('Loading'));

    $.ajax({
        url: '/Account/ReviewPhoto',
        method: 'POST',
        data: { id, action }
    })
        .done(data => {
            hideLoader();

            if (data.success === true) {
                element.closest('.pending-approval').remove();
                updateGalleryList();
                if ($('.pending-approval').length == 0) {
                    updatePendingReviews();
                }
            } else if (data.message) {
                displayMessage(localization.translate('Review'), localization.translate('Review_Failed'), [data.message]);
            }
        })
        .fail((xhr, error) => {
            hideLoader();
            displayMessage(localization.translate('Review'), localization.translate('Review_Failed'), [error]);
        });
}

function updateUsersList() {
    $.ajax({
        type: 'GET',
        url: `/Account/UsersList`,
        success: function (data) {
            $('#users-list').html(data);
        }
    });
}

function updateCustomResources() {
    $.ajax({
        type: 'GET',
        url: `/Account/CustomResources`,
        success: function (data) {
            $('#custom-resources').html(data);
        }
    });
}

function updateSettings() {
    $.ajax({
        type: 'GET',
        url: `/Account/SettingsPartial`,
        success: function (data) {
            $('#settings-list').html(data);
        }
    });
}

function updateAuditList(term = '', limit = 100) {
    clearTimeout(auditSearchTimeout);
    auditSearchTimeout = setTimeout(function () {
        $.ajax({
            type: 'POST',
            url: `/Account/AuditList`,
            data: { term: term?.trim(), limit: limit },
            success: function (data) {
                $('#audit-list').html(data);
            }
        });
    }, 500);
}

function updateGalleryList() {
    $.ajax({
        type: 'GET',
        url: `/Account/GalleriesList`,
        success: function (data) {
            $('#galleries-list').html(data);
        }
    });
}

function updatePendingReviews() {
    $.ajax({
        type: 'GET',
        url: `/Account/PendingReviews`,
        success: function (data) {
            $('#pending-reviews').html(data);
        }
    });
}

function updatePage() {
    updateUsersList();
    updateGalleryList();
    updatePendingReviews();
    updateCustomResources();
    updateSettings();
    updateAuditList();
}

function initPasswordValidation() {
    if ($('.password-validator').length > 0) {
        $('.password-validator').each(function () {
            let validator = $(this);
            let input = $(validator.data('input'));
            if (input !== undefined && input.length > 0) {
                let confirmField = input.parent().parent().parent().find('input.confirm-password');
                if (confirmField !== undefined && confirmField.length === 1) {
                    validator.find('.lbl-confirm').removeClass('visually-hidden');
                    confirmField.off('keyup').on('keyup', function () {
                        var value = $(input).val();
                        setPasswordValidationField(validator.find('.lbl-confirm'), confirmField.val() === value && value.length);
                        setPasswordValidationField(validator, validator.find('li[class^=\'lbl-\']:not([class*=\'hidden\'])').length === 0);
                    });
                }

                $(input).off('keyup').on('keyup', function () {
                    var value = $(this).val();
                    setPasswordValidationField(validator.find('.lbl-lower') , /[a-z]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-upper') , /[A-Z]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-number'), /[0-9]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-special'), /[^A-Za-z0-9]+?/.test(value));
                    setPasswordValidationField(validator.find('.lbl-length'), value.length >= 8);

                    if (confirmField !== undefined && confirmField.length === 1) {
                        setPasswordValidationField(validator.find('.lbl-confirm'), confirmField.val() === value && value.length);
                    }

                    setPasswordValidationField(validator, validator.find('li[class^=\'lbl-\']:not([class*=\'hidden\'])').length === 0);
                })
            }
        });
    }
}

function setPasswordValidationField(field, valid) {
    if (valid) {
        field.addClass('visually-hidden');
    } else {
        field.removeClass('visually-hidden');
    }
}

function checkAccountState() {
    $.ajax({
        url: '/Account/CheckAccountState',
        method: 'GET'
    })
        .done(data => {
            if (data.active !== true) {
                location.href = '/Account/Logout';
            }
        });
}

function selectActiveTab(tab) {
    if (tab === undefined || tab === null || tab.length === 0) {
        tab = $('a.pnl-selector')[0].attributes['data-tab'].value;
    }

    window.location = `/Account?tab=${tab}`;
}

(function () {
    document.addEventListener('DOMContentLoaded', function () {

        clearInterval(accountStateCheckInterval);
        accountStateCheckInterval = setInterval(function () {
            checkAccountState();
        }, 60000);

        $(document).off('click', 'a.pnl-selector').on('click', 'a.pnl-selector', function (e) {
            preventDefaults(e);

            let tab = $(this).data('tab');
            selectActiveTab(tab);
        });

        $(document).off('change', 'input.setting-field,select.setting-field,textarea.setting-field').on('change', 'input.setting-field,select.setting-field,textarea.setting-field', function (e) {
            $(this).attr('data-updated', 'true');
        });

        $(document).off('click', 'button#btnSaveSettings').on('click', 'button#btnSaveSettings', function (e) {
            let updatedFields = $('.setting-field[data-updated="true"]');
            if (updatedFields.length > 0) {
                var settingsList = $.map(updatedFields, function (item) {
                    let element = $(item);
                    return { key: element.data('setting-name'), value: element.val() };
                });

                displayLoader(localization.translate('Loading'));
                $.ajax({
                    url: '/Account/UpdateSettings',
                    method: 'PUT',
                    data: { model: settingsList }
                })
                    .done(data => {
                        if (data.success === true) {
                            displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Success'));
                        } else if (data.message) {
                            displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'), [data.message]);
                        } else {
                            displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'));
                        }
                    })
                    .fail((xhr, error) => {
                        displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'), [error]);
                    });
            } else {
                displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_No_Change'));
            }
        });

        $(document).off('click', '.btnGallerySettings').on('click', '.btnGallerySettings', function (e) {
            preventDefaults(e);

            let galleryId = $(this).data('gallery-id');

            $.ajax({
                type: 'GET',
                url: `/Account/Settings/${galleryId}`,
                success: function (data) {
                    if (data !== undefined) {
                        displayPopup({
                            Title: localization.translate('Gallery_Settings'),
                            CustomHtml: data,
                            Buttons: [{
                                Text: localization.translate('Save'),
                                Class: 'btn-primary',
                                Callback: function () {
                                    let updatedFields = $('.setting-field[data-updated="true"]');
                                    if (updatedFields.length > 0) {
                                        var settingsList = $.map(updatedFields, function (item) {
                                            let element = $(item);
                                            return { key: element.data('setting-name'), value: element.val() };
                                        });

                                        displayLoader(localization.translate('Loading'));
                                        $.ajax({
                                            url: '/Account/UpdateSettings',
                                            method: 'PUT',
                                            data: { model: settingsList, galleryId: galleryId }
                                        })
                                            .done(data => {
                                                if (data.success === true) {
                                                    displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Success'), null, function () {
                                                        window.location.reload();
                                                    });
                                                } else if (data.message) {
                                                    displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'), [data.message]);
                                                } else {
                                                    displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'));
                                                }
                                            })
                                            .fail((xhr, error) => {
                                                displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_Failed'), [error]);
                                            });
                                    } else {
                                        displayMessage(localization.translate('Update_Settings'), localization.translate('Update_Settings_No_Change'));
                                    }
                                }
                            }, {
                                Text: localization.translate('Close')
                            }]
                        });
                    } else {
                        displayMessage(localization.translate('Gallery_Settings'), localization.translate('Gallery_Settings_None'));
                    }
                }
            });
        });

        $(document).off('click', 'i.btnReviewApprove').on('click', 'i.btnReviewApprove', function (e) {
            preventDefaults(e);
            reviewPhoto($(this), 1);
        });

        $(document).off('click', 'i.btnReviewReject').on('click', 'i.btnReviewReject', function (e) {
            preventDefaults(e);
            reviewPhoto($(this), 2);
        });

        $(document).off('click', 'i.btnAddUser').on('click', 'i.btnAddUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let passwordValidation = `<ul class="password-validator" data-input="input#popup-modal-field-user-password"> \
                    <li class="lbl-lower">${localization.translate('Password_Validation_Lower')}</li> \
                    <li class="lbl-upper">${localization.translate('Password_Validation_Upper')}</li> \
                    <li class="lbl-number">${localization.translate('Password_Validation_Numbers')}</li> \
                    <li class="lbl-special">${localization.translate('Password_Validation_Special')}</li> \
                    <li class="lbl-length">${localization.translate('Password_Validation_Length')}</li> \
                    <li class="lbl-confirm visually-hidden">${localization.translate('Password_Validation_Confirm')}</li> \
                </ul><script>initPasswordValidation();</script>`;

            displayPopup({
                Title: localization.translate('User_Create'),
                Fields: [{
                    Id: 'user-name',
                    Name: localization.translate('User_Name'),
                    Hint: localization.translate('User_Name_Hint')
                },
                {
                    Id: 'user-email',
                    Name: localization.translate('User_Email'),
                    Hint: localization.translate('User_Email_Hint')
                },
                {
                    Id: 'user-password',
                    Name: localization.translate('User_Password'),
                    Hint: localization.translate('User_Password_Hint'),
                    Type: "password"
                },
                {
                    Id: 'user-cpassword',
                    Name: localization.translate('User_Confirm_Password'),
                    Hint: localization.translate('User_Confirm_Password_Hint'),
                    Type: "password",
                    Class: 'confirm-password'
                },
                {
                    Id: 'user-level',
                    Name: localization.translate('User_Level'),
                    Hint: localization.translate('User_Level_Hint'),
                    Type: 'select',
                    SelectOptions: [
                        {
                            key: '0',
                            selected: true,
                            value: 'Basic'
                        },
                        {
                            key: '1',
                            selected: false,
                            value: 'Reviewer'
                        },
                        {
                            key: '2',
                            selected: false,
                            value: 'Moderator'
                        },
                        {
                            key: '3',
                            selected: false,
                            value: 'Admin'
                        }
                    ]
                }],
                FooterHtml: passwordValidation,
                Buttons: [{
                    Text: localization.translate('Add'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let username = $('#popup-modal-field-user-name').val();
                        if (username == undefined || username.length == 0) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Missing_Name'));
                            return;
                        }

                        const usernameRegex = /^[a-zA-Z0-9\-\s-_~]+$/;
                        if (!usernameRegex.test(username)) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Name'));
                            return;
                        }

                        let email = $('#popup-modal-field-user-email').val();
                        const emailRegex = /^((?!\.)[\w\-_.]*[^.])(@[\w\-_]+)(\.\w+(\.\w+)?[^.\W])$/;
                        if (email != undefined && email.length > 0 && !emailRegex.test(email)) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Email'));
                            return;
                        }

                        let password = $('#popup-modal-field-user-password').val();
                        if (password == undefined || password.length < 8) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Password'));
                            return;
                        }

                        let cpassword = $('#popup-modal-field-user-cpassword').val();
                        if (password !== cpassword) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_CPassword'));
                            return;
                        }

                        let level = $('#popup-modal-field-user-level').val();
                        if (level == undefined || level.length == 0) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Level'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/AddUser',
                            method: 'POST',
                            data: { Username: username, Email: email, Password: password, CPassword: cpassword, Level: level }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateUsersList();
                                    displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('User_Create'), localization.translate('User_Create_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnAddGallery').on('click', 'i.btnAddGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Gallery_Create'),
                Fields: [{
                    Id: 'gallery-name',
                    Name: localization.translate('Gallery_Name'),
                    Hint: localization.translate('Gallery_Name_Hint')
                }, {
                    Id: 'gallery-key',
                    Name: localization.translate('Gallery_Secret_Key'),
                    Hint: localization.translate('Gallery_Secret_Key_Hint')
                }],
                Buttons: [{
                    Text: localization.translate('Create'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let name = $('#popup-modal-field-gallery-name').val();
                        if (name == undefined || name.length == 0) {
                            displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Missing_Name'));
                            return;
                        }

                        const regex = /^[a-zA-Z0-9\-\s-_~]+$/;
                        if (!regex.test(name)) {
                            displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Invalid_Name'));
                            return;
                        }

                        let key = $('#popup-modal-field-gallery-key').val();

                        $.ajax({
                            url: '/Account/AddGallery',
                            method: 'POST',
                            data: { Id: 0, Name: name, SecretKey: key }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateGalleryList();
                                    displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Create'), localization.translate('Gallery_Create_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnBulkReview').on('click', 'i.btnBulkReview', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            } 

            displayPopup({
                Title: localization.translate('Bulk_Review'),
                Message: localization.translate('Bulk_Review_Message'),
                Buttons: [{
                    Text: localization.translate('Approve'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));
                        
                        $.ajax({
                            url: '/Account/BulkReview',
                            method: 'POST',
                            data: { action: 1 }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    hideLoader();
                                } else if (data.message) {
                                    displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Approve_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Approve_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Approve_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Reject'),
                        Class: 'btn-danger',
                        Callback: function () {
                            displayLoader(localization.translate('Loading'));

                            $.ajax({
                                url: '/Account/BulkReview',
                                method: 'POST',
                                data: { action: 2 }
                            })
                                .done(data => {
                                    if (data.success === true) {
                                        updatePage();
                                        hideLoader();
                                    } else if (data.message) {
                                        displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Reject_Failed'), [data.message]);
                                    } else {
                                        displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Reject_Failed'));
                                    }
                                })
                                .fail((xhr, error) => {
                                    displayMessage(localization.translate('Bulk_Review'), localization.translate('Bulk_Review_Reject_Failed'), [error]);
                                });
                        }
                    }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnImport').on('click', 'i.btnImport', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Import_Data'),
                Fields: [{
                    Id: 'import-file',
                    Name: localization.translate('Import_Data_Backup_File'),
                    Type: 'File',
                    Hint: localization.translate('Import_Data_Backup_Hint'),
                    Accept: '.zip'
                }],
                Buttons: [{
                    Text: localization.translate('Import'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        var files = $('#popup-modal-field-import-file')[0].files;
                        if (files == undefined || files.length == 0) {
                            displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Select_File'));
                            return;
                        }

                        var data = new FormData();
                        data.append('file-0', files[0]);

                        $.ajax({
                            url: '/Account/ImportBackup',
                            method: 'POST',
                            data: data,
                            contentType: false,
                            processData: false
                        })
                            .done(data => {
                                if (data.success === true) {
                                    displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Success'));
                                    window.location.reload();
                                } else if (data.message) {
                                    displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Import_Data'), localization.translate('Import_Data_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnExport').on('click', 'i.btnExport', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Export_Data'),
                Fields: [{
                    Id: 'database',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Database'
                }, {
                    Id: 'uploads',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Uploads'
                }, {
                    Id: 'thumbnails',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Thumbnails'
                }, {
                    Id: 'custom-resources',
                    Type: 'checkbox',
                    Checked: true,
                    Class: 'form-check-input',
                    Label: 'Custom Resources'
                }],
                Buttons: [{
                    Text: localization.translate('Export'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        $.ajax({
                            url: '/Account/ExportBackup',
                            method: 'POST',
                            data: {
                                Database: $('#popup-modal-field-database').is(':checked'),
                                Uploads: $('#popup-modal-field-uploads').is(':checked'),
                                Thumbnails: $('#popup-modal-field-thumbnails').is(':checked'),
                                CustomResources: $('#popup-modal-field-custom-resources').is(':checked')
                            }
                        })
                            .done(data => {
                                hideLoader();

                                if (data.success === true && data.filename) {
                                    window.location.href = data.filename;
                                } else if (data.message) {
                                    displayMessage(localization.translate('Export_Data'), localization.translate('Export_Data_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Export_Data'), localization.translate('Export_Data_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Export_Data'), localization.translate('Export_Data_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnWipeAllGalleries').on('click', 'i.btnWipeAllGalleries', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayPopup({
                Title: localization.translate('Wipe_Data'),
                Message: localization.translate('Wipe_Data_Message'),
                Buttons: [{
                    Text: localization.translate('Wipe'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        $.ajax({
                            url: '/Account/WipeAllGalleries',
                            method: 'DELETE'
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Wipe_Data'), localization.translate('Wipe_Data_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnWipe2FA').on('click', 'i.btnWipe2FA', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('2FA_Setup'),
                Message: localization.translate('2FA_Wipe_Message', { name: row.data('user-name') }),
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Wipe'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('2FA_Setup'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/ResetMultifactorAuthForUser',
                            method: 'DELETE',
                            data: { userId: id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Successfully'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('2FA_Setup'), localization.translate('2FA_Set_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnFreezeUser').on('click', 'i.btnFreezeUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Freeze_User'),
                Message: `${localization.translate('Freeze_User_Message')} '${row.data('user-name') }'`,
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Freeze'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Freeze_User'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/FreezeUser',
                            method: 'PUT',
                            data: { Id: id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Freeze_User'), localization.translate('Freeze_Successfully'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Freeze_User'), localization.translate('Freeze_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Freeze_User'), localization.translate('Freeze_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Freeze_User'), localization.translate('Freeze_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnUnfreezeUser').on('click', 'i.btnUnfreezeUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Unfreeze_User'),
                Message: `${localization.translate('Unfreeze_User_Message')} '${row.data('user-name')}'`,
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Unfreeze'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Unfreeze_User'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/UnfreezeUser',
                            method: 'PUT',
                            data: { Id: id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Unfreeze_User'), localization.translate('Unfreeze_Successfully'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Unfreeze_User'), localization.translate('Unfreeze_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Unfreeze_User'), localization.translate('Unfreeze_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Unfreeze_User'), localization.translate('Unfreeze_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnDeleteUser').on('click', 'i.btnDeleteUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('User_Delete'),
                Message: localization.translate('User_Delete_Message', { name: row.data('user-name') }),
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Delete'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('User_Delete'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/DeleteUser',
                            method: 'DELETE',
                            data: { id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('User_Delete'), localization.translate('User_Delete_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnOpenGallery').on('click', 'i.btnOpenGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            window.open($(this).data('url'), $(this).data('target'));
        });

        $(document).off('click', 'i.btnDownloadGallery').on('click', 'i.btnDownloadGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            displayLoader(localization.translate('Loading'));

            let row = $(this).closest('tr');
            let id = row.data('gallery-id');
            let secretKey = row.data('gallery-key');

            $.ajax({
                url: '/Gallery/DownloadGallery',
                method: 'POST',
                data: { Id: id, SecretKey: secretKey }
            })
                .done(data => {
                    hideLoader();

                    if (data.success === true && data.filename) {
                        window.location.href = data.filename;
                    } else if (data.message) {
                        displayMessage(localization.translate('Download'), localization.translate('Download_Failed'), [data.message]);
                    } else {
                        displayMessage(localization.translate('Download'), localization.translate('Download_Failed'));
                    }
                })
                .fail((xhr, error) => {
                    hideLoader();
                    displayMessage(localization.translate('Download'), localization.translate('Download_Failed'), [error]);
                });
        });

        $(document).off('click', 'i.btnEditGallery').on('click', 'i.btnEditGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Gallery_Edit'),
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }, {
                    Id: 'gallery-name',
                    Name: localization.translate('Gallery_Name'),
                    Value: row.data('gallery-name'),
                    Hint: localization.translate('Gallery_Name_Hint')
                }, {
                    Id: 'gallery-key',
                    Name: localization.translate('Gallery_Secret_Key'),
                    Value: row.data('gallery-key'),
                    Hint: localization.translate('Gallery_Secret_Key_Hint')
                }],
                Buttons: [{
                    Text: localization.translate('Update'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Missing_Id'));
                            return;
                        }

                        let name = $('#popup-modal-field-gallery-name').val();
                        if (name == undefined || name.length == 0) {
                            displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Missing_Name'));
                            return;
                        }

                        let key = $('#popup-modal-field-gallery-key').val();

                        $.ajax({
                            url: '/Account/EditGallery',
                            method: 'PUT',
                            data: { Id: id, Name: name, SecretKey: key }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateGalleryList();
                                    displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Edit'), localization.translate('Gallery_Edit_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnEditUser').on('click', 'i.btnEditUser', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('User_Edit'),
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }, {
                    Id: 'user-name',
                    Name: localization.translate('User_Name'),
                    Value: row.data('user-name'),
                    Hint: localization.translate('User_Name_Hint'),
                    Disabled: true
                }, {
                    Id: 'user-email',
                    Name: localization.translate('User_Email'),
                    Value: row.data('user-email'),
                    Hint: localization.translate('User_Email_Hint')
                }, {
                    Id: 'user-level',
                    Name: localization.translate('User_Level'),
                    Hint: localization.translate('User_Level_Hint'),
                    Type: 'select',
                    SelectOptions: [
                        {
                            key: '0',
                            selected: row.data('user-level') == '0',
                            value: 'Basic'
                        },
                        {
                            key: '1',
                            selected: row.data('user-level') == '1',
                            value: 'Reviewer'
                        },
                        {
                            key: '2',
                            selected: row.data('user-level') == '2',
                            value: 'Moderator'
                        },
                        {
                            key: '3',
                            selected: row.data('user-level') == '3',
                            value: 'Admin'
                        }
                    ]
                }],
                Buttons: [{
                    Text: localization.translate('Update'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('User_Edit'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        let email = $('#popup-modal-field-user-email').val();
                        const emailRegex = /^((?!\.)[\w\-_.]*[^.])(@[\w\-_]+)(\.\w+(\.\w+)?[^.\W])$/;
                        if (email != undefined && email.length > 0 && !emailRegex.test(email)) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Email'));
                            return;
                        }

                        let level = $('#popup-modal-field-user-level').val();
                        if (level == undefined || level.length == 0) {
                            displayMessage(localization.translate('User_Edit'), localization.translate('User_Invalid_Level'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/EditUser',
                            method: 'PUT',
                            data: { Id: id, Email: email, Level: level }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateUsersList();
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnChangePassword').on('click', 'i.btnChangePassword', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let passwordValidation = `<ul class="password-validator" data-input="input#popup-modal-field-user-password"> \
                    <li class="lbl-lower">Lower case letters</li> \
                    <li class="lbl-upper">Upper case letter</li> \
                    <li class="lbl-number">Numbers</li> \
                    <li class="lbl-special">Special characters</li> \
                    <li class="lbl-length">At least 8 characters</li> \
                    <li class="lbl-confirm visually-hidden">Confirm password matches</li> \
                </ul><script>initPasswordValidation();</script>`;

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('User_Edit'),
                Fields: [{
                    Id: 'user-id',
                    Value: row.data('user-id'),
                    Type: 'hidden'
                }, {
                    Id: 'user-password',
                    Name: localization.translate('User_Password'),
                    Value: row.data('user-password'),
                    Hint: localization.translate('User_Password_Hint'),
                    Type: 'password'
                }, {
                    Id: 'user-cpassword',
                    Name: localization.translate('User_Confirm_Password'),
                    Value: row.data('user-cpassword'),
                    Hint: localization.translate('User_Confirm_Password_Hint'),
                    Type: 'password',
                    Class: 'confirm-password'
                }],
                FooterHtml: passwordValidation,
                Buttons: [{
                    Text: localization.translate('Update'),
                    Class: 'btn-success',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-user-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('User_Edit'), localization.translate('User_Missing_Id'));
                            return;
                        }

                        let password = $('#popup-modal-field-user-password').val();
                        if (password == undefined || password.length < 8) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_Password'));
                            return;
                        }

                        let cpassword = $('#popup-modal-field-user-cpassword').val();
                        if (password == undefined || password !== cpassword) {
                            displayMessage(localization.translate('User_Create'), localization.translate('User_Invalid_CPassword'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/ChangeUserPassword',
                            method: 'PUT',
                            data: { Id: id, Password: password, CPassword: cpassword }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updateUsersList();
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('User_Edit'), localization.translate('User_Edit_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnWipeGallery').on('click', 'i.btnWipeGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Gallery_Wipe'),
                Message: localization.translate('Gallery_Wipe_Message', { name: row.data('gallery-name') }),
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Wipe'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/WipeGallery',
                            method: 'DELETE',
                            data: { id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Wipe'), localization.translate('Gallery_Wipe_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('click', 'i.btnDeleteGallery').on('click', 'i.btnDeleteGallery', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let row = $(this).closest('tr');
            displayPopup({
                Title: localization.translate('Gallery_Delete'),
                Message: localization.translate('Gallery_Delete_Message', { name: row.data('gallery-name') }),
                Fields: [{
                    Id: 'gallery-id',
                    Value: row.data('gallery-id'),
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Delete'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-gallery-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Missing_Id'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/DeleteGallery',
                            method: 'DELETE',
                            data: { id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    updatePage();
                                    displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Success'));
                                } else if (data.message) {
                                    displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Gallery_Delete'), localization.translate('Gallery_Delete_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('change', 'input#custom-resource-upload').on('change', 'input#custom-resource-upload', function (e) {
            const files = $(this)[0].files;
            if (files !== undefined && files.length > 0) {
                const formData = new FormData();
                formData.append(files[0].name, files[0]);

                displayLoader(`${localization.translate('Upload_Progress', { index: i + 1, count: 1 })} <br/><br/><span id="file-upload-progress">0%</span>`);

                $.ajax({
                    url: '/Account/UploadCustomResource',
                    type: 'POST',
                    data: formData,
                    async: true,
                    cache: false,
                    contentType: false,
                    dataType: 'json',
                    processData: false,
                    success: function (response) {
                        hideLoader();
                        if (response !== undefined && response.success === true) {
                            displayMessage(localization.translate('Upload'), localization.translate('Upload_Success', { count: 1 }));

                            updateCustomResources();
                            updateSettings();

                            $('input#custom-resource-upload').val('');
                        } else if (response.errors !== undefined && response.errors.length > 0) {
                            displayMessage(localization.translate('Upload'), localization.translate('Upload_Failed'), [response.errors]);
                        }
                    },
                    error: function (response) {
                        console.log(response);
                        displayMessage(localization.translate('Upload'), localization.translate('Upload_Failed'), errors);
                    },
                    xhr: function () {
                        var xhr = new window.XMLHttpRequest();

                        xhr.upload.addEventListener("progress", function (evt) {
                            if (evt.lengthComputable) {
                                var percentComplete = evt.loaded / evt.total;
                                percentComplete = parseInt(percentComplete * 100);

                                if ($('span#file-upload-progress').length > 0) {
                                    $('span#file-upload-progress').text(`(${percentComplete}%)`);
                                }
                            }
                        }, false);

                        return xhr;
                    },
                });
            }
        });

        $(document).off('click', 'i.custom-resource-delete').on('click', 'i.custom-resource-delete', function (e) {
            preventDefaults(e);

            if ($(this).attr('disabled') == 'disabled') {
                return;
            }

            let id = $(this).data('id');
            let name = $(this).data('name');
            let element = $(this).closest('.custom-resource');

            displayPopup({
                Title: localization.translate('Delete_Item'),
                Message: localization.translate('Delete_Item_Message', { name }),
                Fields: [{
                    Id: 'custom-resource-id',
                    Value: id,
                    Type: 'hidden'
                }],
                Buttons: [{
                    Text: localization.translate('Delete'),
                    Class: 'btn-danger',
                    Callback: function () {
                        displayLoader(localization.translate('Loading'));

                        let id = $('#popup-modal-field-custom-resource-id').val();
                        if (id == undefined || id.length == 0) {
                            displayMessage(localization.translate('Delete_Item'), localization.translate('Delete_Item_Id_Missing'));
                            return;
                        }

                        $.ajax({
                            url: '/Account/RemoveCustomResource',
                            method: 'DELETE',
                            data: { id }
                        })
                            .done(data => {
                                if (data.success === true) {
                                    displayMessage(localization.translate('Delete_Item'), localization.translate('Delete_Item_Success'));

                                    updateCustomResources();
                                    updateSettings();

                                    element.remove();
                                } else if (data.message) {
                                    displayMessage(localization.translate('Delete_Item'), localization.translate('Delete_Item_Failed'), [data.message]);
                                } else {
                                    displayMessage(localization.translate('Delete_Item'), localization.translate('Delete_Item_Failed'));
                                }
                            })
                            .fail((xhr, error) => {
                                displayMessage(localization.translate('Delete_Item'), localization.translate('Delete_Item_Failed'), [error]);
                            });
                    }
                }, {
                    Text: localization.translate('Close')
                }]
            });
        });

        $(document).off('keyup', 'input#audit-log-search-term, input#audit-log-limit').on('keyup', 'input#audit-log-search-term, input#audit-log-limit', function (e) {
            let term = $('input#audit-log-search-term').val();
            let limit = $('input#audit-log-limit').val();
            
            updateAuditList(term, limit);
        });
    });
})();