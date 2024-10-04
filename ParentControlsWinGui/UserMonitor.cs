using System;
using System.Management;
using System.DirectoryServices.AccountManagement;

namespace ParentControlsWinGui
{
    internal class UserMonitor
    {
        public UserMonitor()
        {
        }

        public static LinkedList<String> ListUserAccounts()
        {
            LinkedList<string> userAccounts = new LinkedList<string>();

            try
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                {
                    UserPrincipal user = new UserPrincipal(context);
                    PrincipalSearcher searcher = new PrincipalSearcher(user);

                    foreach (var found in searcher.FindAll())
                    {
                        userAccounts.AddLast(found.SamAccountName); // Store usernames in the linked list
                    }
                }
            }
            catch (ManagementException e)
            {
                // delete try catch block if using PrincipalContext
                MessageBox.Show($"An error occurred: {e.Message}");
            }

            return userAccounts;
        }

        public static string getCurrentUser()
        {
            return Environment.UserName;
        }
    }
}
