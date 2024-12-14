using UnityEngine;
using System.Collections;

public class LinkedList
{
	public class Node
	{
		public Node m_Prev;
		public Node m_Next;
	}

	Node m_Null = new Node();

	public LinkedList()
	{
		m_Null.m_Next = m_Null;
		m_Null.m_Prev = m_Null;
	}

	public Node GetFirst()
	{
		return m_Null.m_Next;
	}

	public Node GetLast()
	{
		return m_Null.m_Prev;
	}

	public Node GetEnd()
	{
		return m_Null;
	}

	public void PushBack(Node node)
	{
		Node prev = m_Null.m_Prev;
		
		prev.m_Next = node;
		node.m_Prev = prev;
		
		node.m_Next = m_Null;
		m_Null.m_Prev = node;
	}

	public Node PopBack()
	{
		if (!IsEmpty())
		{
			Node node = m_Null.m_Prev;
			Node prev = node.m_Prev;

			if (prev != null)
			{
				prev.m_Next = m_Null;
				m_Null.m_Prev = prev;
			}

			node.m_Prev = null;
			node.m_Next = null;

			return node;
		}
		return null;
	}
	
	public void PushFront(Node node)
	{
		Node next = m_Null.m_Next;
		
		next.m_Prev = node;
		node.m_Next = next;
		
		node.m_Prev = m_Null;
		m_Null.m_Next = node;
	}

	public Node PopFront()
	{
		if (!IsEmpty())
		{
			Node node = m_Null.m_Next;
			Node next = node.m_Next;

			next.m_Prev = m_Null;
			m_Null.m_Next = next;

			node.m_Prev = null;
			node.m_Next = null;

			return node;
		}
		return null;
	}
	
	public void Remove(Node node)
	{
		Node prev = node.m_Prev;
		Node next = node.m_Next;
		
		prev.m_Next = next;
		next.m_Prev = prev;
		
		node.m_Prev = null;
		node.m_Next = null;
	}

	public bool IsEmpty()
	{
		return (m_Null.m_Prev == m_Null && m_Null.m_Next == m_Null);
	}
}