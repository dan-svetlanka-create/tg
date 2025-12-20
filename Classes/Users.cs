using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TelegramBot_Dan.Classes
{
    public class Users
    {
        public int Id { get; set; }
        public long IdUser { get; set; }
        public List<Events> Events { get; set; }
       
        public Users(long idUser)
        {
            IdUser = idUser;
            Events = new List<Events>();
          
        }

    }
}

