import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { toast } from 'sonner'
import { IconEye, IconEyeOff } from '@tabler/icons-react'
import { authService } from '../services/authService'
import { getErrorMessage } from '../lib/errors'

const schema = z.object({
  username: z.string().min(1, 'Username is required'),
  password: z.string().min(1, 'Password is required'),
})

type FormValues = z.infer<typeof schema>

/** Login page: username/password form that exchanges credentials for a JWT token pair. */
export function LoginPage() {
  const navigate = useNavigate()
  const [showPassword, setShowPassword] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const login = useMutation({
    mutationFn: authService.login,
    onSuccess: () => {
      toast.success('Welcome back')
      navigate('/')
    },
    onError: (err) => toast.error(getErrorMessage(err, 'Login failed')),
  })

  return (
    <span className="flex min-h-screen">
      <aside
        className="hidden w-[40%] flex-col justify-center px-12 lg:flex"
        style={{ background: 'var(--charcoal)', color: 'var(--cream)' }}
      >
        <p className="brand m-0 text-6xl leading-none">
          boutique<span style={{ color: 'var(--gold)' }}>iq</span>
        </p>
        <p className="m-0 mt-4 text-lg opacity-70">inventory · warehouse · monitoring</p>
      </aside>

      <main
        className="flex flex-1 items-center justify-center p-6"
        style={{ background: 'var(--cream)' }}
      >
        <form
          onSubmit={handleSubmit((v) => login.mutate(v))}
          className="card w-full max-w-[360px]"
        >
          <p className="brand m-0 mb-1 text-3xl lg:hidden">
            boutique<span style={{ color: 'var(--gold)' }}>iq</span>
          </p>
          <h1 className="page-title m-0 mb-6 text-2xl">Sign in</h1>

          <label className="mb-4 block text-left text-sm">
            <span className="mb-1 block text-[var(--text-muted)]">Username</span>
            <input className="input-field" autoComplete="username" {...register('username')} />
            {errors.username && (
              <span className="mt-1 block text-xs text-[var(--danger-text)]">{errors.username.message}</span>
            )}
          </label>

          <label className="mb-6 block text-left text-sm">
            <span className="mb-1 block text-[var(--text-muted)]">Password</span>
            <span className="relative block">
              <input
                type={showPassword ? 'text' : 'password'}
                className="input-field pr-10"
                autoComplete="current-password"
                {...register('password')}
              />
              <button
                type="button"
                className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-[var(--text-muted)]"
                onClick={() => setShowPassword((s) => !s)}
                tabIndex={-1}
              >
                {showPassword ? <IconEyeOff size={18} /> : <IconEye size={18} />}
              </button>
            </span>
            {errors.password && (
              <span className="mt-1 block text-xs text-[var(--danger-text)]">{errors.password.message}</span>
            )}
          </label>

          <button type="submit" className="btn-primary w-full" disabled={login.isPending}>
            {login.isPending ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </main>
    </span>
  )
}
