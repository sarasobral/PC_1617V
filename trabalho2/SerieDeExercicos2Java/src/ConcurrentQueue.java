/***
 *
 *  ISEL, LEIC, Concurrent Programming, Verão 2016/17
 *
 *	Carlos Martins
 *
 *  Codigo anexo ao exercício 1 da SE#2
 *
 ***/

import java.util.Random;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.*;

public class ConcurrentQueue<T> {
    private static class Node<W> {
        public final W item;
        public final AtomicReference<Node<W>> next;
        public Node(W item, Node<W> next) {
            this.item = item;
            this.next = new AtomicReference<Node<W>>(next);
        }
    }

    private final Node<T> dummy = new Node<T>(null, null);
    // pontam para o mesmo nó
    private final AtomicReference<Node<T>> head = new AtomicReference<>(dummy);
    private final AtomicReference<Node<T>> tail = new AtomicReference<>(dummy);

    // coloca no fim da fila o elemento passado como argumento
    public void put(T item) {
        Node<T> newNode = new Node<T>(item, null);
        while (true) {
            // observo o valor da tail e tail.next porque quero adicionar à tail
            Node<T> curTail = tail.get();
            Node<T> tailNext = curTail.next.get();
            if (curTail == tail.get()) {
                if (tailNext != null) {
                    // ja foi colocado um elemento antes de mim avançar com o tail = nextTail
                    tail.compareAndSet(curTail, tailNext); //advance tail
                } else {
                    // nao foi colocado nenhum elemento antes de mim tail.next = elemento
                    if (curTail.next.compareAndSet(null, newNode)) {
                        tail.compareAndSet(curTail, newNode);
                        return;
                    }
                }
            }
        }
    }

    // retorna o elemento presente no início da fila ou null, no caso da fila se encontrar vazia
    public T tryTake() {
        while (true) {
            Node<T> headCur = head.get();
            Node<T> tailCur = tail.get();
            Node<T> headNext = headCur.next.get();
            if (headCur == head.get()) {
                if (headCur == tailCur) {
                    // a fila está vazia
                    if (headNext == null) return null;
                    tail.compareAndSet(tailCur, headNext);
                } else {
                    // ler o  valor antes de CAS
                    T val = headNext.item;
                    // avançar uma posiçao na lista é colocado head=nextHead porque o head é considerado dummy */
                    if (head.compareAndSet(headCur, headNext)) return val;
                }
            }
        }
    }

    // take an item - spinning if necessary
    public T take() throws InterruptedException {
        T v;
        while ((v = tryTake()) == null) {
            Thread.sleep(0);
        }
        return v;
    }

    // indica se a fila está vazia
    public boolean isEmpty() {
        return head.get() == tail.get();
    }
}