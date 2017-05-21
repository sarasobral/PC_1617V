/***
 *
 *  ISEL, LEIC, Concurrent Programming, Verão 2016/17
 *
 *	Carlos Martins
 *
 *  Codigo anexo ao exercício 2 da SE#2
 * 
 ***/

import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;

public class ThrottledRegion_ {	
	
	private class ThrottledRegionForKey {
		public ThrottledRegionForKey() {
			// ...
		}
		
		public boolean tryEnter() throws InterruptedException {
			// ...
		}
		
		public void leave() {
			// ...
		}
	}
	
	private final ConcurrentMap<Integer, ThrottledRegionForKey> keyToRegion = new ConcurrentHashMap<>();
	
	public ThrottledRegion(int maxInside, int maxWaiting, int waitTimeout) {
		// ...
	}
	
	public boolean tryEnter(int key) throws InterruptedException {
		return keyToRegion.computeIfAbsent(key, k -> new ThrottledRegionForKey()).tryEnter();
	}
	
	public void leave(int key){
		keyToRegion.get(key).leave();
	}
}
