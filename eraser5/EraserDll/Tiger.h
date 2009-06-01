// tiger.h
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


#ifndef TIGER_H
#define TIGER_H

/*
** If you use these, you of course know that state is 24 bytes and that
** the block size of tiger_compress is 64 bytes
*/

void tiger_compress(E_PUINT64 block, E_PUINT64 state);

/*
** Tiger needs one-block (64 bytes) work buffer, if you don't provide it,
** one will be allocated for you.
*/

void tiger(E_PUINT64 buffer, E_UINT64 length, E_PUINT64 state, E_PUINT8 work);
void tiger(E_PUINT64 buffer, E_UINT64 length, E_PUINT64 state);

#endif
