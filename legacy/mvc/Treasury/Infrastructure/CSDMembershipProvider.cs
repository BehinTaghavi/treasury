using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using CSD.Treasury.DataModel;

namespace CSD.Treasury.UI.Infrastructure
{
    public class CSDMembershipProvider : MembershipProvider
    {
        string _appName = "";
        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            _appName = config.Get("applicationName");
            base.Initialize(name, config);
        }
        public override string ApplicationName
        {
            get
            {
                return _appName;
            }
            set
            {
                _appName = value;
            }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            var db = new TreasuryEntities();
            {
                var user = new UserInfo()
                {
                    UserName = username,
                    UserPassword = password,
                    IsAdmin = bool.Parse(email),
                    User_FullName = passwordQuestion,
                    User_FullName2 = passwordAnswer,
                    IsActive = isApproved,
                };
                if (string.IsNullOrEmpty(password))
                    status = MembershipCreateStatus.InvalidPassword;
                else
                {
                    db.UserInfoes.Add(user);
                    db.SaveChanges();
                    status = MembershipCreateStatus.Success;
                    return GetMembershipUserFromUser(user);
                }
                return null;
            }
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        public override bool EnablePasswordReset
        {
            get { throw new NotImplementedException(); }
        }

        public override bool EnablePasswordRetrieval
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            var dc = new TreasuryEntities();
            var user = dc.UserInfoes.SingleOrDefault(u => u.UserName == username);
            if (user != null)
                return GetMembershipUserFromUser(user);
            else return null;
        }
        private MembershipUser GetMembershipUserFromUser(UserInfo usr)
        {
            MembershipUser u = null;
            u = new MembershipUser(this.Name, usr.UserName, usr.UserID, usr.User_FullName, string.Empty, string.Empty, usr.IsActive, true, usr.CreatedTime, usr.LastLoginTime.Value, DateTime.Now, DateTime.Now, DateTime.Now);
            return u;
        }
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { throw new NotImplementedException(); }
        }

        public override int MinRequiredPasswordLength
        {
            get { throw new NotImplementedException(); }
        }

        public override int PasswordAttemptWindow
        {
            get { throw new NotImplementedException(); }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { throw new NotImplementedException(); }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new NotImplementedException(); }
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        public override bool ValidateUser(string username, string password)
        {
            var dc = new TreasuryEntities();
            {
                var pass = CSD.Common.Encryption.Hash.HashPassword(password);
                return dc.UserInfoes.Any(u => u.UserName == username && u.UserPassword == pass && u.IsActive);
            }

        }
    }

    public class CSDRoleProvider : RoleProvider
    {
        string _appNameq = "";
        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            _appNameq = config.Get("applicationName");
            base.Initialize(name, config);
        }
        public override string ApplicationName
        {
            get
            {
                return _appNameq;
            }
            set
            {
                _appNameq = value;
            }
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            using (var db = new TreasuryEntities())
            {
                UserInfo user = db.UserInfoes.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.CurrentCultureIgnoreCase));
                var userRole = db.UserInRoles.Select(d => d.RoleId);
                var roles = db.RolesInfoes.Where(q => userRole.Contains(q.RoleID)).Select(d => d.RoleName).ToList();
                //var roles = from ur in user.RolesInfoes
                //            select ur.RoleName;
                if (roles != null)
                    return roles.ToArray();
                else
                    return new string[] { }; ;
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            using (var db = new TreasuryEntities())
            {
                UserInfo user = db.UserInfoes.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.CurrentCultureIgnoreCase));
                var userRole = db.UserInRoles.Select(d => d.RoleId);
                var roles = db.RolesInfoes.Where(q => userRole.Contains(q.RoleID)).Select(d => d.RoleName).ToList();
                //var roles = from ur in user.RolesInfoes
                //            select ur.RoleName;
                if (user != null)
                    return roles.Any(r => r.Equals(roleName, StringComparison.CurrentCultureIgnoreCase));
                else
                    return false;
            }
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}