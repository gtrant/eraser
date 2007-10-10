/*
Module : SortedArray.h
Purpose: Interface for an MFC template class which provides sorting and ordered insertion
         derived from the MFC class CArray
Created: PJN / 25-12-1999
History: PJN / 12-01-2000 Fixed a stack overflow in CSortedArray::Sort
         PJN / 21-02-2000 Fixed a number of problems in CSortedArray::Find
         PJN / 22-02-2000 Fixed a problem in CSortedArray::Find when there are no items in the array
         PJN / 29-02-2000 Fixed a problem in CSortedArray::Sort when there are no items in the array
         PJN / 27-08-2000 1. Fixed another stack overflow problem in CSortedArray::Sort.
                          2. Fixed a problem in CSortedArray::Sort where the comparison function
                          was returning negative values, 0 and positive values instead of -1, 0 & 1.
                          Thanks to Ted Crow for finding both of these problems.
         PJN / 01-10-2001 Fixed another bug in Sort!. Thanks to Jim Johnson for spotting this.
         PJN / 29-05-2002 1. Fixed a problem in CSortedArray::OrderedInsert. Thanks to John Young
                          for spotting and fixing this problem.
                          2. Updated copyright and usage instructions
         PJN / 06-12-2002 1. Rewrote the Sort method following reports of further problems by 
                          Serhiy Pavlov and Brian Rhodes
         PJN / 11-12-2002 1. Optimized code by replacing all calls to CArray<>::ElementAt with CArray<>::GetData


Copyright (c) 1999 - 2002 by PJ Naughter.  (Web: www.naughter.com, Email: pjna@naughter.com)

All rights reserved.

Copyright / Usage Details:

You are allowed to include the source code in any product (commercial, shareware, freeware or otherwise) 
when your product is released in binary form. You are allowed to modify the source code in any way you want 
except you cannot modify the copyright details at the top of each module. If you want to distribute source 
code with your application, then you are only allowed to distribute versions released by the author. This is 
to maintain a single distribution point for the source code. 

*/


////////////////////////////////// Macros ///////////////////////////

#ifndef __SORTEDARRAY_H__
#define __SORTEDARRAY_H__

#ifndef __AFXTEMPL_H__
#include <afxtempl.h> 
#pragma message("To avoid this message, please put afxtempl.h in your PCH")
#endif



/////////////////////////// Classes /////////////////////////////////

//Class which implements sorting for its parent class CArray

template<class TYPE, class ARG_TYPE>
class CSortedArray : public CArray<TYPE, ARG_TYPE>
{
public:
//Constructors / Destructors
  CSortedArray();

//Typedefs
  typedef int COMPARE_FUNCTION(ARG_TYPE element1, ARG_TYPE element2); 
  typedef COMPARE_FUNCTION* LPCOMPARE_FUNCTION;

//Methods
  int  OrderedInsert(ARG_TYPE newElement, int nCount=1);
  void Sort(int nLowIndex=0, int nHighIndex=-1);
  int  Find(ARG_TYPE element, int nLowIndex=0, int nHighIndex=-1);
  void SetCompareFunction(LPCOMPARE_FUNCTION pCompareFunction) { ASSERT(pCompareFunction); m_pCompareFunction = pCompareFunction; };
  LPCOMPARE_FUNCTION GetCompareFunction() const { return m_pCompareFunction; };

protected:
  LPCOMPARE_FUNCTION m_pCompareFunction;
};

template<class TYPE, class ARG_TYPE>
CSortedArray<TYPE, ARG_TYPE>::CSortedArray()
{
  m_pCompareFunction = NULL; 
}

template<class TYPE, class ARG_TYPE>
int CSortedArray<TYPE, ARG_TYPE>::OrderedInsert(ARG_TYPE newElement, int nCount)
{
	ASSERT(m_pCompareFunction); 	//Did you forget to call SetCompareFunction prior to calling this function?

	int lo = 0;
	int hi = GetUpperBound();
  TYPE* pData = GetData();
	
	//Find the insert location (mid) for the new element.
	int mid = hi / 2;
	while (hi >= lo)
	{
    ASSERT(pData);  
		int res = m_pCompareFunction(newElement, pData[mid]);
		if (res == 0)
			break;

		if (res < 0)
			hi = mid - 1;	//Insert in the lower half...
		else 
      lo = mid + 1; //Insert in the upper half...
		mid = (hi - lo) / 2 + lo;
	}
	
	InsertAt(mid, newElement, nCount);
	return mid;
}

template<class TYPE, class ARG_TYPE>
int CSortedArray<TYPE, ARG_TYPE>::Find(ARG_TYPE element, int nLowIndex, int nHighIndex)
{
  ASSERT(m_pCompareFunction != NULL); //Did you forget to call SetCompareFunction prior to calling this function

  //If there are no items in the array, then return immediately
  if (GetSize() == 0)
    return -1;

  int left = nLowIndex;
  int right = nHighIndex;
  if (right == -1)
    right = GetUpperBound();
  ASSERT(left <= right);

  TYPE* pData = GetData();
  ASSERT(pData);

  if (left == right) //Straight comparision fewer than 2 elements to search
  {
    BOOL bFound = (m_pCompareFunction(pData[left], element) == 0);
    if (bFound)
      return left;
    else
      return -1;
  }

  //do a binary chop to find the location where the element should be inserted
  int nFoundIndex = -1;
  while (nFoundIndex == -1 && left != right)
  {
    int nCompareIndex;
    if (right == (left+2))
      nCompareIndex = left+1;
    else
      nCompareIndex = ((right - left)/2) + left;

    int nCompare = m_pCompareFunction(pData[nCompareIndex], element);
    switch (nCompare)
    {
      case -1:
      {
        if ((right - left) == 1)
        {
          if (m_pCompareFunction(pData[right], element) == 0)
            nFoundIndex = right;
          else if (m_pCompareFunction(pData[left], element) == 0)
            nFoundIndex = left;
          else
            left = right;
        }
        else
          left = nCompareIndex;
        break;
      }
      case 0:
      {
        nFoundIndex = nCompareIndex;
        break;
      }
      case 1:
      {
        if ((right - left) == 1)
        {
          if (m_pCompareFunction(pData[right], element) == 0)
            nFoundIndex = right;
          else if (m_pCompareFunction(pData[left], element) == 0)
            nFoundIndex = left;
          else
            right = left;
        }
        else
          right = nCompareIndex;
        break;
      }
      default:
      {
        ASSERT(FALSE); //Your compare function has been coded incorrectly. It should
                       //return -1, 0 or 1 similiar to the way the C Runtime function
                       //"qsort" works
        break;
      }
    }
  }
  
  return nFoundIndex;
}

template<class TYPE, class ARG_TYPE>
void CSortedArray<TYPE, ARG_TYPE>::Sort(int nLowIndex, int nHighIndex)
{
  //Sort all the data?
	if (nHighIndex == -1)
    nHighIndex = GetUpperBound();     

  //quick exit                  
  if ((nLowIndex == nHighIndex) || (GetSize() == 0))
    return;               
  
  //Validate the required values for this function
  ASSERT(nHighIndex <= GetUpperBound());
  ASSERT(m_pCompareFunction);

  //Do the actual quicksort  
  if (nLowIndex < nHighIndex)
  {    
    int i = nLowIndex;
    int j = nHighIndex;
    TYPE* pData = GetData();
    ASSERT(pData);
    TYPE center = pData[(nLowIndex + nHighIndex)/2];
    while (i <= j)
    {
      while ((m_pCompareFunction(pData[i], center) < 0) && (i < nHighIndex)) 
        i++;
      while ((m_pCompareFunction(pData[j], center) > 0) && (j > nLowIndex)) 
        j--;
    
      if (i<=j)
      {
        TYPE x  = pData[i];
        pData[i] = pData[j];
        pData[j] = x;
        i++; 
        j--;
      }
    } 
  
    if (nLowIndex  < j) 
      Sort(nLowIndex , j);
    if (nHighIndex > i) 
      Sort(i, nHighIndex);
  }
}

#endif //__SORTEDARRAY_H__
