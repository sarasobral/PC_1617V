using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SerieDeExercicos2Csharp {
    public class ConcurrentQueue<T> {
        class Node<W> {
            public readonly W item;
            public volatile Node<W> next;
            public Node(W item, Node<W> next)
            {
                this.item = item;
                this.next = next;
            }
        }

        private readonly static Node<T> dummy = new Node<T>(default(T), null);
        // pontam para o mesmo nó
        private volatile Node<T> head = dummy;
        private volatile Node<T> tail = dummy;

        // coloca no fim da fila o elemento passado como argumento
        public void Put(T item)
        {
            Node<T> newNode = new Node<T>(item, null);
            while (true)
            {
                // observo o valor da tail e tail.next porque quero adicionar à tail
                Node<T> currentTail = tail;
                Node<T> tailNext = currentTail.next;
                if (currentTail == tail)
                {
                    if (tailNext != null)
                    // ja foi colocado um elemento antes de mim avançar com o tail = nextTail
                        Interlocked.CompareExchange(ref tail, tailNext, currentTail);
                    else
                    {
                        // nao foi colocado nenhum elemento antes de mim tail.next = elemento
                        if (Interlocked.CompareExchange(ref currentTail.next, newNode, null) == null)
                        {
                            Interlocked.CompareExchange(ref tail, newNode, currentTail);
                            return;
                        }
                    }
                }
            }
        }


        // retorna o elemento presente no início da fila ou null, no caso da fila se encontrar vazia
        public T TryTake()
        {
            while (true)
            {
                Node<T> currentHead = head;
                Node<T> currentTail = tail;
                Node<T> nextHead = currentHead.next;
                if (currentHead == head)
                {
                    if (currentHead == currentTail)
                    { 
                        // a fila está vazia
                        if (nextHead == null) 
                            return default(T); 
                        Interlocked.CompareExchange(ref tail, nextHead, currentTail);
                    }
                    else
                    {
                        // ler o  valor antes de CAS
                        T val = nextHead.item;
                        // avançar uma posiçao na lista é colocado head=nextHead porque o head é considerado dummy */
                        if (Interlocked.CompareExchange(ref head, nextHead, currentHead) == currentHead)
                            return val;
                    }
                }
            }
        }
        
        // Take an item - spinning if necessary
        public T Take()
        {
            T v;
            while ((v = TryTake()) == null)
            {
                Thread.Sleep(0);
            }

            return v;
        }

        // indica se a fila está vazia
        public bool IsEmpty()
        {
            return head == tail;
        }

    }

}