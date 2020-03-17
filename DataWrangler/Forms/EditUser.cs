﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DataWrangler.DBOs;
using DataWrangler.FormControls;
using DataWrangler.Retrievers;
using MetroFramework.Components;
using MetroFramework.Controls;
using MetroFramework.Forms;

namespace DataWrangler.Forms
{
    public partial class EditUser : MetroForm
    {
        private readonly Dictionary<string, string> _dbSettings;
        private readonly UserAccount _user;
        private  UserAccount _userObj;

        private DataCache _dataCacheAuditHistory;
        private IDataRetriever _retrieverAuditHistory;

        private DataCache _dataCacheHistory;
        private IDataRetriever _retrieverHistory;

        private bool passwordModified;
   
        public EditUser(Dictionary<string, string> dbSettings, UserAccount user, UserAccount userObj)
        {
            InitializeComponent();
            StyleHelper.LoadFormSavedStyle(this);
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, tabUser,
                new object[] { true });

            _dbSettings = dbSettings;
            _user = user;
            _userObj = userObj;

            if (_userObj == null)
            {
                tabControl.TabPages.Remove(tabHistory);
                tabControl.TabPages.Remove(tabAuditHistory);
            }
            else
            {
                LoadUser();
            }
            tabControl.SelectedTab = tabUser;
            BringToFront();
        }

        private void LoadUser()
        {
            txtUserId.Text = _userObj.Id.ToString();
            txtUpdated.Text = _userObj.LastUpdated.ToShortDateString();
            togActiveStat.Checked = _userObj.Active;
            txtUsername.Text = _userObj.Username;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        
        private void btnSave_Click(object sender, EventArgs e)
        {
            bool needsDbAction = false;

            var txtUser = txtUsername.Text;
            bool active = togActiveStat.Checked;
            var txtPass = txtPassword.Text;

            if (_userObj == null)
            {
                needsDbAction = true;
            }
            else
            {
                if (!txtUser.Equals(_userObj.Username))
                {
                    _userObj.Username = txtUser;
                    needsDbAction = true;
                }

                if (active != _userObj.Active)
                {
                    _userObj.Active = active;
                    needsDbAction = true;
                }

                if (passwordModified)
                {
                    _userObj.Password = UserAccount.GetPasswordHash(txtPass);
                    needsDbAction = true;
                }
            }


            

            if (needsDbAction)
            {
                StatusObject dbActionStatus;
                using (var oH = new ObjectHelper(_dbSettings, _user))
                {
                    if (_userObj == null)
                    {
                        dbActionStatus = oH.AddUserAccount(txtUser, txtPass, active);
                        if (dbActionStatus.Success)
                        {
                            DialogResult = DialogResult.OK;
                            Close();
                        }
                        else
                        {
                            MessageBox.Show("Failed to add user!");
                        }
                    }
                    else
                    {
                        dbActionStatus = oH.UpdateUserAccount(_userObj);
                        if (dbActionStatus.Success)
                        {
                            DialogResult = DialogResult.OK;
                            Close();
                        }
                        else
                        {
                            MessageBox.Show("Failed to update user!");
                        }
                    }
                    
                }
            }
        }

        private void gridAuditHistory_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            e.Value = _dataCacheAuditHistory.RetrieveElement(e.RowIndex, e.ColumnIndex);
        }

        public void LoadAuditHistory()
        {
            try
            {
                _retrieverAuditHistory = new AuditEntryRetriever(_dbSettings, _userObj, true);
                _dataCacheAuditHistory = new DataCache(_retrieverAuditHistory, 500);

                gridAuditHistory.Columns.Clear();
                gridAuditHistory.Rows.Clear();

                foreach (var column in _retrieverAuditHistory.Columns)
                    gridAuditHistory.Columns.Add(column, column);

                gridAuditHistory.RowCount = _retrieverAuditHistory.RowCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered: " + ex.Message);
            }
        }

        public void LoadHistory()
        {
            try
            {
                _retrieverHistory = new AuditEntryRetriever(_dbSettings, _userObj);
                _dataCacheHistory = new DataCache(_retrieverHistory, 500);

                gridHistory.Columns.Clear();
                gridHistory.Rows.Clear();

                foreach (var column in _retrieverHistory.Columns)
                    gridHistory.Columns.Add(column, column);

                gridHistory.RowCount = _retrieverHistory.RowCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered: " + ex.Message);
            }
        }

        private void VerifyPasswords()
        {
            btnSave.Enabled = txtPassword.Text.Equals(txtPasswordVerify.Text);
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            if (txtPassword.Text.Length > 0)
            {
                txtPasswordVerify.Enabled = true;
                passwordModified = true;
                VerifyPasswords();
            }
            else
            {
                txtPasswordVerify.Enabled = false;
                txtPasswordVerify.Text = "";
                passwordModified = false;
            }
        }

        private void txtPasswordVerify_TextChanged(object sender, EventArgs e)
        {
            if(passwordModified)
                VerifyPasswords();
        }

        private void gridHistory_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            e.Value = _dataCacheHistory.RetrieveElement(e.RowIndex, e.ColumnIndex);
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabHistory && _retrieverHistory == null)
            {

                LoadHistory();
                
            }
            else if (tabControl.SelectedTab == tabAuditHistory && _retrieverAuditHistory == null)
            {
                LoadAuditHistory();
            }
        }
    }
}