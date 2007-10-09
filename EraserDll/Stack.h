// Stack.h
//
// Eraser. Secure data removal. For Windows.
// Copyright © 1997-2001  Sami Tolvanen (sami@tolvanen.com).
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
// 02111-1307, USA.

#ifndef STACK_H
#define STACK_H

// template of a simple stack (LIFO = Last In First Out)

template <class T> class CStack
{
public:
    CStack();
    ~CStack();

    bool        Push(const T&);
    bool        Pop(T*);

    bool        IsEmpty();

protected:
    T           m_tValue;
    CStack<T>   *m_psNext;
};

template <class T> CStack<T>::CStack() :
m_psNext(0)
{
}

template <class T> CStack<T>::~CStack()
{
    try {
        if (m_psNext) {
            delete m_psNext;
            m_psNext = 0;
        }
    } catch (...) {
    }
}

template <class T> bool CStack<T>::Push(const T& tValue)
{
    try {
        CStack<T> *psNew = new CStack<T>();
        psNew->m_tValue  = tValue;

        psNew->m_psNext = m_psNext;
        m_psNext = psNew;
        return true;
    } catch (...) {
        return false;
    }
}

template <class T> bool CStack<T>::Pop(T* ptValue)
{
    try {
        if (ptValue && m_psNext) {
            CStack<T> *psNext = m_psNext->m_psNext;

            *ptValue = m_psNext->m_tValue;

            m_psNext->m_psNext = 0;
            delete m_psNext;

            m_psNext = psNext;
            return true;
        }
    } catch (...) {
    }

    return false;
}

template <class T> bool CStack<T>::IsEmpty()
{
    return (m_psNext != 0);
}

#endif