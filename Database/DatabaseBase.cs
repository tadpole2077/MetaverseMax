﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // abstract class type = must be inherited by a class, cant be used standalone
    public abstract class DatabaseBase : DBLogger
    {

        public readonly MetaverseMaxDbContext _context;

        // Protected base method, can only be accessed via code(methods) from same class or derived class. 
        protected DatabaseBase(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            _context = _parentContext;            
        }

    }
}