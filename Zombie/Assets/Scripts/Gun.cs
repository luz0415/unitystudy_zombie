using System.Collections;
using UnityEngine;

// 총을 구현한다
public class Gun : MonoBehaviour {
    // 총의 상태를 표현하는데 사용할 타입을 선언한다
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄창이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 총알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 총알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기
    public AudioClip shotClip; // 발사 소리
    public AudioClip reloadClip; // 재장전 소리

    public float damage = 25; // 공격력
    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄약
    public int magCapacity = 25; // 탄창 용량
    public int magAmmo; // 현재 탄창에 남아있는 탄약


    public float timeBetFire = 0.12f; // 총알 발사 간격
    public float reloadTime = 1.8f; // 재장전 소요 시간
    private float lastFireTime; // 총을 마지막으로 발사한 시점


    private void Awake() {
        // 사용할 컴포넌트들의 참조를 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        // 사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;
        // 라인 렌더러 비활성화
        bulletLineRenderer.enabled = false;
    }

    private void OnEnable() {
        // 탄창 가득 채우기
        magAmmo = magCapacity;
        // 현재 상태를 쏠 준비가 된 상태로 변경
        state = State.Ready;
        // 마지막으로 쏜 시점 초기화
        lastFireTime = 0;
    }

    // 발사 시도
    public void Fire() {
        // 현재 발사 가능한 상태면서 총 발사시점에서 timeBetFire만큼의 시간이 지났다면
        if(state == State.Ready && Time.time >= lastFireTime + timeBetFire) 
        {
            // 마지막 발사 시점 갱신
            lastFireTime = Time.time;
            // 발사
            Shot();
        }

    }

    // 실제 발사 처리
    private void Shot() {
        // 레이캐스트 충돌 정보 저장하는 변수
        RaycastHit hit;
        // 맞은 곳 저장하는 변수
        Vector3 hitPosition = Vector3.zero;

        // 레이캐스트(시작점, 방향, 충돌정보, 거리)
        if(Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance))
        {
            // 어떤 물체하고 충돌한 경우
            // 충돌한 상대로부터 IDamageable 가져오기
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if(target != null)
            {
                // IDamageable의 OnDamage 함수 실행시켜 데미지 주기
                target.OnDamage(damage, hit.point, hit.normal);
            }

            // 레이가 충돌한 위치 저장
            hitPosition = hit.point;
        }
        else
        {
            // 레이가 충돌하지 않았다면
            // 최대 사정거리까지 날아갔을 때가 충돌위치
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        }

        // 발사 이펙트 재생
        StartCoroutine(ShotEffect(hitPosition));

        // 탄 -1
        magAmmo--;
        if(magAmmo <= 0)
        {
            // 남은 탄알 없다면 Empty로 상태 갱신
            state = State.Empty;
        }
    }

    // 발사 이펙트와 소리를 재생하고 총알 궤적을 그린다
    private IEnumerator ShotEffect(Vector3 hitPosition) {
        // 총구 화염 & 탄피 배출 효과 재생
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();
        
        // 총격 소리 재생
        gunAudioPlayer.PlayOneShot(shotClip);

        // 선의 시작점과 끝점 지정
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);

        // 라인 렌더러를 활성화하여 총알 궤적을 그린다
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 총알 궤적을 지운다
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        if(state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            // 지금 재장전 중이거나, 탄알이 없거나, 탄약이 가득한 경우
            // 재장전 불가
            return false;
        }

        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;
        // 재장전 소리 재생
        gunAudioPlayer.PlayOneShot(reloadClip);

        // 재장전 소요 시간 만큼 처리를 쉬기
        yield return new WaitForSeconds(reloadTime);

        // 채울 탄약 계산
        int ammoToFill = magCapacity - magAmmo;

        // 탄창에 채워야 할 탄알이 남은 탄알보다 많다면
        if(ammoRemain < ammoToFill)
        {
            ammoToFill = ammoRemain;
        }

        // 탄창을 채움
        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}